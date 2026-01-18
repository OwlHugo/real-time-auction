namespace RealTimeAuction.Application.Auctions.Commands.CreateAuction;

public record CreateAuctionCommand : IRequest<int>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CreateAuctionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        var duration = request.EndTime - request.StartTime;
        if (duration.TotalMinutes < 1)
        {
            throw new RealTimeAuction.Application.Common.Exceptions.ValidationException(
                "O leilão deve ter duração mínima de 1 minuto"
            );
        }

        if (request.StartTime < DateTime.UtcNow)
        {
            request.StartTime = DateTime.UtcNow.AddSeconds(5);
        }

        var auction = new Auction
        {
            Title = request.Title,
            Description = request.Description,
            StartingPrice = request.StartingPrice,
            CurrentPrice = request.StartingPrice,
            ReservePrice = request.ReservePrice,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            SellerId = _currentUser.Id!,
            Status = AuctionStatus.Scheduled,
        };

        _context.Auctions.Add(auction);
        await _context.SaveChangesAsync(cancellationToken);
        return auction.Id;
    }
}
