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

    private IClientProxy Group(int auctionId) =>
        _hubContext.Clients.Group(AuctionHub.GroupName(auctionId));

    public Task NotifyBidPlaced(BidPlacedNotification n) =>
        Group(n.AuctionId)
            .SendAsync(
                "BidPlaced",
                new
                {
                    n.BidId,
                    n.AuctionId,
                    n.Amount,
                    n.BidderId,
                    n.BidderName,
                    n.Sequence,
                    n.Timestamp,
                }
            );

    public Task NotifyBidRejected(int auctionId, decimal amount, string bidderName, string reason) =>
        Group(auctionId)
            .SendAsync(
                "BidRejected",
                new
                {
                    AuctionId = auctionId,
                    Amount = amount,
                    BidderName = bidderName,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow,
                }
            );

    public Task NotifyAuctionStarted(int auctionId) =>
        Group(auctionId).SendAsync("AuctionStarted", new { AuctionId = auctionId });

    public Task NotifyAuctionEnded(
        int auctionId,
        string? winnerId,
        string? winnerName,
        decimal finalPrice
    ) =>
        Group(auctionId)
            .SendAsync(
                "AuctionEnded",
                new
                {
                    AuctionId = auctionId,
                    WinnerId = winnerId,
                    WinnerName = winnerName,
                    FinalPrice = finalPrice,
                }
            );
}
