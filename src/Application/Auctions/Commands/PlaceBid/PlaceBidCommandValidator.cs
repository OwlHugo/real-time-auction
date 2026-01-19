namespace RealTimeAuction.Application.Auctions.Commands.PlaceBid;

public class PlaceBidCommandValidator : AbstractValidator<PlaceBidCommand>
{
    public PlaceBidCommandValidator()
    {
        RuleFor(v => v.AuctionId).GreaterThan(0);

        RuleFor(v => v.Amount).GreaterThan(0).WithMessage("Bid amount must be positive.");
    }
}
