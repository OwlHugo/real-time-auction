namespace RealTimeAuction.Application.Auctions.Commands.PlaceBid;

public record PlaceBidCommand : IRequest<int>
{
    public int AuctionId { get; set; }
    public decimal Amount { get; set; }
}

public class PlaceBidCommandHandler : IRequestHandler<PlaceBidCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private static readonly Prometheus.Counter BidsCounter = Prometheus.Metrics.CreateCounter(
        "app_bids_total",
        "Total de lances recebidos"
    );
    private static readonly Prometheus.Histogram BidAmountHistogram =
        Prometheus.Metrics.CreateHistogram(
            "app_bid_amount",
            "Distribuição de valores de lances",
            new Prometheus.HistogramConfiguration
            {
                Buckets = Prometheus.Histogram.LinearBuckets(start: 100, width: 100, count: 10),
            }
        );

    public PlaceBidCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        var auction = await _context
            .Auctions.Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        if (auction == null)
        {
            throw new RealTimeAuction.Application.Common.Exceptions.NotFoundException(
                "Leilão não encontrado"
            );
        }

        const decimal minimumIncrement = 10m;
        if (request.Amount < auction.CurrentPrice + minimumIncrement)
        {
            throw new RealTimeAuction.Application.Common.Exceptions.ValidationException(
                $"O lance deve ser pelo menos R$ {minimumIncrement} maior que o preço atual (R$ {auction.CurrentPrice})"
            );
        }

        if (!auction.CanPlaceBid(request.Amount))
        {
            throw new RealTimeAuction.Application.Common.Exceptions.ValidationException(
                "Valor do lance deve ser maior que o preço atual e leilão deve estar ativo"
            );
        }

        var bid = new Bid
        {
            AuctionId = request.AuctionId,
            BidderId = _currentUser.Id!,
            Amount = request.Amount,
            PlaceAt = DateTime.UtcNow,
            Status = BidStatus.Accepted,
        };

        var previousBid = auction.Bids.Where(b => b.Status == BidStatus.Accepted);
        foreach (var prevBid in previousBid)
        {
            prevBid.Status = BidStatus.Outbid;
        }
        auction.CurrentPrice = request.Amount;
        _context.Bids.Add(bid);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            BidsCounter.Inc();
            BidAmountHistogram.Observe((double)request.Amount);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RealTimeAuction.Application.Common.Exceptions.ValidationException(
                "O preço do leilão mudou enquanto você tentava dar o lance. Tente novamente."
            );
        }
        return bid.Id;
    }
}
