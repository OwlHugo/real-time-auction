namespace RealTimeAuction.Application.Auctions.Commands.GetAuctionById;

public record GetAuctionByIdQuery : IRequest<AuctionDetailDto>
{
    public int Id { get; init; }
}

public class GetAuctionByIdQueryHandler : IRequestHandler<GetAuctionByIdQuery, AuctionDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAuctionByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<AuctionDetailDto> Handle(
        GetAuctionByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var auction = await _context
            .Auctions.Include(a => a.Bids.OrderByDescending(b => b.PlaceAt).Take(10))
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (auction == null)
        {
            throw new RealTimeAuction.Application.Common.Exceptions.NotFoundException(
                nameof(Auction),
                request.Id
            );
        }

        return _mapper.Map<AuctionDetailDto>(auction);
    }
}
