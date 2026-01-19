using RealTimeAuction.Domain.Entities;

namespace RealTimeAuction.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Auction> Auctions { get; }
    DbSet<Bid> Bids { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
