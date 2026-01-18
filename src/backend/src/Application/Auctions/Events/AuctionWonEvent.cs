namespace RealTimeAuction.Application.Auctions.Events;

public class AuctionWonEvent
{
    public int AuctionId { get; set; }
    public string WinnerId { get; set; } = string.Empty;
}
