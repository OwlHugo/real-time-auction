using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealTimeAuction.Application.Auctions.Events;
using RealTimeAuction.Application.Common.Interfaces;
using RealTimeAuction.Domain.Entities;
using RealTimeAuction.Domain.Enums;

namespace RealTimeAuction.Infrastructure.BackgroundJobs;

/// <summary>
/// Abre e encerra leilões no horário. Roda em TODAS as instâncias do servidor ao mesmo tempo,
/// então duas instâncias podem tentar encerrar o mesmo leilão. A exclusão mútua entre os nós
/// vem do mesmo controle otimista dos lances: cada leilão é salvo individualmente com
/// "WHERE RowVersion = @lida"; quem perder recebe DbUpdateConcurrencyException e simplesmente
/// desiste. As notificações e o evento no RabbitMQ só saem DEPOIS do commit, então o
/// vencedor é anunciado exatamente uma vez, mesmo com N instâncias.
/// </summary>
public class AuctionStatusWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionStatusWorker> _logger;

    public AuctionStatusWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AuctionStatusWorker> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Erro no AuctionStatusWorker.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        List<int> toStart;
        List<int> toEnd;

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            toStart = await context
                .Auctions.Where(a =>
                    a.Status == AuctionStatus.Scheduled && a.StartTime <= now && a.EndTime > now
                )
                .Select(a => a.Id)
                .ToListAsync(ct);
            toEnd = await context
                .Auctions.Where(a =>
                    (a.Status == AuctionStatus.Active || a.Status == AuctionStatus.Scheduled)
                    && a.EndTime <= now
                )
                .Select(a => a.Id)
                .ToListAsync(ct);
        }

        foreach (var id in toStart)
        {
            await TryTransitionAsync(id, StartAsync, ct);
        }

        foreach (var id in toEnd)
        {
            await TryTransitionAsync(id, EndAsync, ct);
        }
    }

    private async Task TryTransitionAsync(
        int auctionId,
        Func<IServiceProvider, Auction, CancellationToken, Task<Func<Task>?>> transition,
        CancellationToken ct
    )
    {
        // Um escopo (DbContext) por leilão: um conflito em um não atrapalha os outros.
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var auction = await context.Auctions.FirstOrDefaultAsync(a => a.Id == auctionId, ct);
        if (auction == null)
            return;

        var afterCommit = await transition(scope.ServiceProvider, auction, ct);
        if (afterCommit == null)
            return;

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Outra instância (ou um lance de último segundo) alterou o leilão antes.
            // Não fazemos nada: o próximo ciclo relê o estado atualizado.
            _logger.LogInformation(
                "Leilão {Id}: outra instância/lance alterou primeiro; transição ignorada neste nó.",
                auctionId
            );
            return;
        }

        await afterCommit();
    }

    private Task<Func<Task>?> StartAsync(IServiceProvider sp, Auction auction, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (auction.Status != AuctionStatus.Scheduled || auction.StartTime > now || auction.EndTime <= now)
            return Task.FromResult<Func<Task>?>(null);

        auction.Status = AuctionStatus.Active;
        var hub = sp.GetRequiredService<IAuctionHubService>();

        return Task.FromResult<Func<Task>?>(async () =>
        {
            _logger.LogInformation("Leilão {Id} ABERTO.", auction.Id);
            await hub.NotifyAuctionStarted(auction.Id);
        });
    }

    private async Task<Func<Task>?> EndAsync(IServiceProvider sp, Auction auction, CancellationToken ct)
    {
        if (
            auction.Status is not (AuctionStatus.Active or AuctionStatus.Scheduled)
            || auction.EndTime > DateTime.UtcNow
        )
            return null;

        var context = sp.GetRequiredService<IApplicationDbContext>();
        var winningBid = await context
            .Bids.Where(b => b.AuctionId == auction.Id)
            .OrderByDescending(b => b.Amount)
            .ThenBy(b => b.Sequence)
            .FirstOrDefaultAsync(ct);

        auction.Status = AuctionStatus.Ended;

        var reserveMet = winningBid != null
            && (auction.ReservePrice == null || winningBid.Amount >= auction.ReservePrice);
        if (reserveMet)
        {
            auction.WinnerId = winningBid!.BidderId;
            auction.WinnerName = winningBid.BidderName;
        }

        var hub = sp.GetRequiredService<IAuctionHubService>();
        var publisher = sp.GetRequiredService<IMessagePublisher>();

        return async () =>
        {
            _logger.LogInformation(
                "Leilão {Id} ENCERRADO. Vencedor: {Winner}",
                auction.Id,
                auction.WinnerName ?? "(nenhum)"
            );
            await hub.NotifyAuctionEnded(
                auction.Id,
                auction.WinnerId,
                auction.WinnerName,
                auction.CurrentPrice
            );

            if (auction.WinnerId != null)
            {
                try
                {
                    // Processamento assíncrono (e-mail ao vencedor) via fila no RabbitMQ.
                    // Com o broker fora do ar, PublishAsync pode ficar esperando para sempre.
                    // O limite de tempo impede que ele trave o worker e os outros leilões.
                    await publisher
                        .PublishAsync(
                            new AuctionWonEvent
                            {
                                AuctionId = auction.Id,
                                WinnerId = auction.WinnerId,
                            },
                            "email-queue"
                        )
                        .WaitAsync(PublishTimeout);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao publicar AuctionWonEvent do leilão {Id}.", auction.Id);
                }
            }
        };
    }
}
