using RealTimeAuction.Domain.Entities;
using RealTimeAuction.Domain.Enums;

namespace RealTimeAuction.Application.Auctions.Commands.GetAuctions;

public class AuctionDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal StartingPrice { get; init; }
    public decimal? ReservePrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public AuctionStatus Status { get; init; }
    public string SellerId { get; init; } = string.Empty;

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Auction, AuctionDto>();
        }
    }
}
