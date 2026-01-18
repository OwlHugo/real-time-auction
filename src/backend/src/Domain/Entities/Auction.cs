using System.ComponentModel.DataAnnotations;
using RealTimeAuction.Domain.Enums;

namespace RealTimeAuction.Domain.Entities;

public class Auction : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AuctionStatus Status { get; set; }

    public string? WinnerId { get; set; }

    public string SellerId { get; set; } = string.Empty;

    public ICollection<Bid> Bids { get; set; } = new List<Bid>();

    [ConcurrencyCheck]
    public Guid RowVersion { get; set; } = Guid.NewGuid();

    public bool IsActive() =>
        Status == AuctionStatus.Active
        && DateTime.UtcNow >= StartTime
        && DateTime.UtcNow <= EndTime;

    public bool CanPlaceBid(decimal bidAmount) => IsActive() && bidAmount > CurrentPrice;
}
