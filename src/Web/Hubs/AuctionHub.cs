using Microsoft.AspNetCore.SignalR;

namespace RealTimeAuction.Web.Hubs;

public class AuctionHub : Hub
{
    public async Task JoinAuction(int auctionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
        await Clients
            .Group($"auction-{auctionId}")
            .SendAsync("UserJoined", Context.User?.Identity?.Name ?? "Anonymous");
    }

    public async Task LeaveAuction(int auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
        await Clients
            .Group($"auction-{auctionId}")
            .SendAsync("UserLeft", Context.User?.Identity?.Name ?? "Anonymous");
    }

    public async Task SendBidNotification(int auctionId, decimal amount, string bidderName)
    {
        await Clients
            .Group($"auction-{auctionId}")
            .SendAsync(
                "BidPlaced",
                new
                {
                    AuctionId = auctionId,
                    Amount = amount,
                    BidderName = bidderName,
                    Timestamp = DateTime.UtcNow,
                }
            );
    }
}
