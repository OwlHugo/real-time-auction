using Microsoft.AspNetCore.SignalR;
using RealTimeAuction.Application.Common.Interfaces;
using RealTimeAuction.Web.Hubs;

namespace RealTimeAuction.Web.Services;

public class AuctionHubService : IAuctionHubService
{
    private readonly IHubContext<AuctionHub> _hubContext;

    public AuctionHubService(IHubContext<AuctionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyBidPlaced(
        int auctionId,
        int bidId,
        decimal amount,
        string bidderId,
        DateTime timestamp
    )
    {
        await _hubContext
            .Clients.Group($"auction-{auctionId}")
            .SendAsync(
                "BidPlaced",
                new
                {
                    BidId = bidId,
                    AuctionId = auctionId,
                    Amount = amount,
                    BidderId = bidderId,
                    Timestamp = timestamp,
                }
            );
    }

    public async Task NotifyAuctionStarted(int auctionId)
    {
        await _hubContext
            .Clients.Group($"auction-{auctionId}")
            .SendAsync("AuctionStarted", new { AuctionId = auctionId });
    }

    public async Task NotifyAuctionEnded(int auctionId, string? winnerId)
    {
        await _hubContext
            .Clients.Group($"auction-{auctionId}")
            .SendAsync("AuctionEnded", new { AuctionId = auctionId, WinnerId = winnerId });
    }
}
