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
    public string SellerName { get; init; } = string.Empty;
    public long Sequence { get; init; }
    public int BidCount { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Auction, AuctionDto>()
                .ForMember(d => d.BidCount, opt => opt.MapFrom(s => (int)s.Sequence));
        }
    }
}
