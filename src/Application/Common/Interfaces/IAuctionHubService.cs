namespace RealTimeAuction.Application.Common.Interfaces;

public record BidPlacedNotification(
    int AuctionId,
    int BidId,
    decimal Amount,
    string BidderId,
    string BidderName,
    long Sequence,
    DateTime Timestamp
);

public interface IAuctionHubService
{
    Task NotifyBidPlaced(BidPlacedNotification notification);
    Task NotifyBidRejected(int auctionId, decimal amount, string bidderName, string reason);
    Task NotifyAuctionStarted(int auctionId);
    Task NotifyAuctionEnded(int auctionId, string? winnerId, string? winnerName, decimal finalPrice);
}
