using RealTimeAuction.Application.Common.Models;

namespace RealTimeAuction.Application.Auctions.Commands.GetAuctions;

public record GetAuctionsQuery : IRequest<PaginatedList<AuctionDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public AuctionStatus? Status { get; init; }
    public string? SearchTerm { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? SortBy { get; init; } = "created";
    public bool SortDescending { get; init; } = true;
}

public class GetAuctionsQueryHandler : IRequestHandler<GetAuctionsQuery, PaginatedList<AuctionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAuctionsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<AuctionDto>> Handle(
        GetAuctionsQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = _context.Auctions.AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(searchTerm)
                || a.Description.ToLower().Contains(searchTerm)
            );
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(a => a.CurrentPrice >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(a => a.CurrentPrice <= request.MaxPrice.Value);
        }

        query = request.SortBy?.ToLower() switch
        {
            "price" => request.SortDescending
                ? query.OrderByDescending(a => a.CurrentPrice)
                : query.OrderBy(a => a.CurrentPrice),
            "title" => request.SortDescending
                ? query.OrderByDescending(a => a.Title)
                : query.OrderBy(a => a.Title),
            "endtime" => request.SortDescending
                ? query.OrderByDescending(a => a.EndTime)
                : query.OrderBy(a => a.EndTime),
            _ => request.SortDescending
                ? query.OrderByDescending(a => a.Created)
                : query.OrderBy(a => a.Created),
        };

        return await PaginatedList<AuctionDto>.CreateAsync(
            query.Select(a => _mapper.Map<AuctionDto>(a)),
            request.Page,
            request.PageSize,
            cancellationToken
        );
    }
}
