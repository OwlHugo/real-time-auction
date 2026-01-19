namespace RealTimeAuction.Application.Common.Interfaces;

public interface IAuctionHubService
{
    Task NotifyBidPlaced(
        int auctionId,
        int bidId,
        decimal amount,
        string bidderId,
        DateTime timestamp
    );
    Task NotifyAuctionStarted(int auctionId);
    Task NotifyAuctionEnded(int auctionId, string? winnerId);
}
