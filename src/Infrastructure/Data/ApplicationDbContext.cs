using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RealTimeAuction.Application.Common.Interfaces;
using RealTimeAuction.Domain.Entities;
using RealTimeAuction.Infrastructure.Identity;

namespace RealTimeAuction.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly AuditInterceptor? _auditInterceptor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditInterceptor auditInterceptor
    )
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _auditInterceptor?.LogChanges(this);

        foreach (var entry in ChangeTracker.Entries<Auction>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid();
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Auction>(entity =>
        {
            entity.Property(a => a.Title).IsRequired().HasMaxLength(200);
            entity.Property(a => a.StartingPrice).HasPrecision(18, 2);
            entity.Property(a => a.ReservePrice).HasPrecision(18, 2);
            entity.Property(a => a.CurrentPrice).HasPrecision(18, 2);

            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.WinnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Bid>(entity =>
        {
            entity.Property(b => b.Amount).HasPrecision(18, 2);

            entity
                .HasOne(b => b.Auction)
                .WithMany(a => a.Bids)
                .HasForeignKey(b => b.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(b => b.BidderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(b => new { b.AuctionId, b.PlaceAt });
        });
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
