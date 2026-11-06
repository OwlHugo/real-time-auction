using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace RealTimeAuction.Web.Hubs;

/// <summary>
/// Hub de tempo real. A contagem de espectadores fica no Redis (conjunto por leilão),
/// então vale para TODAS as instâncias do servidor: quem entrou no nó A aparece para quem
/// está no nó B. Os grupos do SignalR também passam pelo backplane do Redis.
/// </summary>
public class AuctionHub(IConnectionMultiplexer redis) : Hub
{
    private const string TrackedAuctionsItem = "tracked-auctions";
    private static readonly TimeSpan PresenceTtl = TimeSpan.FromHours(1);

    public static string GroupName(int auctionId) => $"auction-{auctionId}";

    private static string PresenceKey(int auctionId) => $"presence:auction:{auctionId}";

    public async Task JoinAuction(int auctionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(auctionId));

        var db = redis.GetDatabase();
        await db.SetAddAsync(PresenceKey(auctionId), Context.ConnectionId);
        await db.KeyExpireAsync(PresenceKey(auctionId), PresenceTtl);

        TrackedAuctions().Add(auctionId);
        await BroadcastPresenceAsync(auctionId);
    }

    public async Task LeaveAuction(int auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(auctionId));
        TrackedAuctions().Remove(auctionId);
        await RemovePresenceAsync(auctionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var auctionId in TrackedAuctions().ToList())
        {
            await RemovePresenceAsync(auctionId);
        }
        TrackedAuctions().Clear();

        await base.OnDisconnectedAsync(exception);
    }

    private HashSet<int> TrackedAuctions()
    {
        if (Context.Items[TrackedAuctionsItem] is HashSet<int> set)
            return set;

        set = new HashSet<int>();
        Context.Items[TrackedAuctionsItem] = set;
        return set;
    }

    private async Task RemovePresenceAsync(int auctionId)
    {
        await redis.GetDatabase().SetRemoveAsync(PresenceKey(auctionId), Context.ConnectionId);
        await BroadcastPresenceAsync(auctionId);
    }

    private async Task BroadcastPresenceAsync(int auctionId)
    {
        var viewers = await redis.GetDatabase().SetLengthAsync(PresenceKey(auctionId));
        await Clients
            .Group(GroupName(auctionId))
            .SendAsync("PresenceChanged", new { AuctionId = auctionId, Viewers = viewers });
    }
}
