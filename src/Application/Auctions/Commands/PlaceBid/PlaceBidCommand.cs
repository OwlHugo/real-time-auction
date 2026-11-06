using NotFoundException = RealTimeAuction.Application.Common.Exceptions.NotFoundException;
using ValidationException = RealTimeAuction.Application.Common.Exceptions.ValidationException;

namespace RealTimeAuction.Application.Auctions.Commands.PlaceBid;

public record PlaceBidCommand : IRequest<PlaceBidResult>
{
    public int AuctionId { get; init; }
    public decimal Amount { get; init; }
}

public record PlaceBidResult(int Id, int AuctionId, decimal Amount, long Sequence, DateTime PlacedAt);

public class PlaceBidCommandHandler : IRequestHandler<PlaceBidCommand, PlaceBidResult>
{
    public const decimal MinimumIncrement = 10m;

    private static string FormatBrl(decimal value) =>
        value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly IAuctionHubService _hub;

    private static readonly Prometheus.Counter BidsCounter = Prometheus.Metrics.CreateCounter(
        "app_bids_total",
        "Total de lances aceitos"
    );
    private static readonly Prometheus.Counter ConflictsCounter = Prometheus.Metrics.CreateCounter(
        "app_bid_conflicts_total",
        "Lances rejeitados por conflito de concorrência (controle otimista)"
    );
    private static readonly Prometheus.Histogram BidAmountHistogram =
        Prometheus.Metrics.CreateHistogram(
            "app_bid_amount",
            "Distribuição de valores de lances",
            new Prometheus.HistogramConfiguration
            {
                Buckets = Prometheus.Histogram.LinearBuckets(start: 100, width: 100, count: 10),
            }
        );

    public PlaceBidCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        IAuctionHubService hub
    )
    {
        _context = context;
        _currentUser = currentUser;
        _hub = hub;
    }

    public async Task<PlaceBidResult> Handle(
        PlaceBidCommand request,
        CancellationToken cancellationToken
    )
    {
        var bidderId = _currentUser.Id ?? throw new UnauthorizedAccessException();
        var bidderName = _currentUser.DisplayName ?? "Anônimo";

        // Lê o leilão junto com sua versão (RowVersion). Nenhum lock é mantido aqui:
        // a exclusão mútua acontece no UPDATE condicional feito pelo SaveChanges.
        var auction = await _context.Auctions.FirstOrDefaultAsync(
            a => a.Id == request.AuctionId,
            cancellationToken
        );

        if (auction == null)
        {
            throw new NotFoundException(nameof(Auction), request.AuctionId);
        }

        if (auction.SellerId == bidderId)
        {
            throw new ForbiddenAccessException("Você não pode dar lance no seu próprio leilão.");
        }

        if (!auction.IsActive())
        {
            throw new ValidationException("Este leilão não está aberto para lances.");
        }

        if (request.Amount < auction.CurrentPrice + MinimumIncrement)
        {
            throw new ValidationException(
                $"O lance deve ser pelo menos {FormatBrl(MinimumIncrement)} maior que o preço atual ({FormatBrl(auction.CurrentPrice)})."
            );
        }

        var previousWinners = await _context
            .Bids.Where(b => b.AuctionId == auction.Id && b.Status == BidStatus.Accepted)
            .ToListAsync(cancellationToken);
        foreach (var previous in previousWinners)
        {
            previous.Status = BidStatus.Outbid;
        }

        var now = DateTime.UtcNow;
        auction.CurrentPrice = request.Amount;
        auction.Sequence++;

        var bid = new Bid
        {
            AuctionId = auction.Id,
            BidderId = bidderId,
            BidderName = bidderName,
            Amount = request.Amount,
            PlaceAt = now,
            Status = BidStatus.Accepted,
            Sequence = auction.Sequence,
        };
        _context.Bids.Add(bid);

        try
        {
            // UPDATE "Auctions" SET ..., "RowVersion" = @nova WHERE "Id" = @id AND "RowVersion" = @lida
            // + INSERT do lance, tudo na mesma transação. Se outro lance gravou antes,
            // 0 linhas são afetadas → DbUpdateConcurrencyException → rollback completo.
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            ConflictsCounter.Inc();
            await _hub.NotifyBidRejected(
                auction.Id,
                request.Amount,
                bidderName,
                "Outro lance foi gravado primeiro"
            );
            throw new ConflictException(
                "Outro participante deu um lance no mesmo instante e chegou primeiro. Confira o novo preço e tente de novo."
            );
        }

        BidsCounter.Inc();
        BidAmountHistogram.Observe((double)request.Amount);

        // Só notifica DEPOIS do commit: nenhum celular vê um lance que não existe no banco.
        await _hub.NotifyBidPlaced(
            new BidPlacedNotification(
                auction.Id,
                bid.Id,
                bid.Amount,
                bidderId,
                bidderName,
                bid.Sequence,
                now
            )
        );

        return new PlaceBidResult(bid.Id, auction.Id, bid.Amount, bid.Sequence, now);
    }
}
