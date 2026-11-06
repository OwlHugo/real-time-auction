namespace RealTimeAuction.Domain.Entities;

public class Bid : BaseEntity
{
    public decimal Amount { get; set; }
    public DateTime PlaceAt { get; set; }
    public BidStatus Status { get; set; }

    /// <summary>Valor do relógio lógico do leilão no momento em que este lance foi aceito.</summary>
    public long Sequence { get; set; }

    public int AuctionId { get; set; }
    public Auction Auction { get; set; } = null!;

    public string BidderId { get; set; } = string.Empty;
    public string BidderName { get; set; } = string.Empty;
}
