namespace RealTimeAuction.Application.Auctions.Commands.GetAuctionById;

public class AuctionDetailDto
{
    public int Id { get; set; }
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
    public int BidCount { get; set; }
    public List<BidDto> RecentBids { get; set; } = new();
}

public class BidDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PlaceAt { get; set; }
    public string BidderId { get; set; } = string.Empty;
    public BidStatus Status { get; set; }
}

public class AuctionDetailDtoMappingProfile : Profile
{
    public AuctionDetailDtoMappingProfile()
    {
        CreateMap<Auction, AuctionDetailDto>()
            .ForMember(d => d.BidCount, opt => opt.MapFrom(s => s.Bids.Count))
            .ForMember(
                d => d.RecentBids,
                opt => opt.MapFrom(s => s.Bids.OrderByDescending(b => b.PlaceAt).Take(10))
            );

        CreateMap<Bid, BidDto>();
    }
}
