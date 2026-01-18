using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealTimeAuction.Application.Auctions.Events;
using RealTimeAuction.Application.Common.Interfaces;
using RealTimeAuction.Domain.Enums;

namespace RealTimeAuction.Infrastructure.BackgroundJobs;

public class AuctionStatusWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<AuctionStatusWorker> _logger;

    public AuctionStatusWorker(
        IServiceScopeFactory scopeFactory,
        IMessagePublisher publisher,
        ILogger<AuctionStatusWorker> logger
    )
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var hubService = scope.ServiceProvider.GetRequiredService<IAuctionHubService>();

                var now = DateTime.UtcNow;

                var scheduledAuctions = await context
                    .Auctions.Where(a => a.Status == AuctionStatus.Scheduled && a.StartTime <= now)
                    .ToListAsync(stoppingToken);

                foreach (var auction in scheduledAuctions)
                {
                    auction.Status = AuctionStatus.Active;
                    _logger.LogInformation("Auction {Id} is now ACTIVE.", auction.Id);

                    await hubService.NotifyAuctionStarted(auction.Id);
                }

                var activeAuctions = await context
                    .Auctions.Include(a => a.Bids)
                    .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
                    .ToListAsync(stoppingToken);

                foreach (var auction in activeAuctions)
                {
                    auction.Status = AuctionStatus.Ended;
                    _logger.LogInformation("Auction {Id} has ENDED.", auction.Id);

                    if (auction.Bids.Any())
                    {
                        var winningBid = auction.Bids.OrderByDescending(b => b.Amount).First();
                        auction.WinnerId = winningBid.BidderId;

                        await hubService.NotifyAuctionEnded(auction.Id, auction.WinnerId);

                        await _publisher.PublishAsync(
                            new AuctionWonEvent
                            {
                                AuctionId = auction.Id,
                                WinnerId = auction.WinnerId,
                            },
                            "email-queue"
                        );
                        _logger.LogInformation(
                            "Winner for auction {Id} is {WinnerId}.",
                            auction.Id,
                            auction.WinnerId
                        );
                    }
                    else
                    {
                        await hubService.NotifyAuctionEnded(auction.Id, null);
                    }
                }

                if (scheduledAuctions.Any() || activeAuctions.Any())
                {
                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in AuctionStatusWorker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
