using NUnit.Framework;
using RealTimeAuction.Domain.Entities;
using RealTimeAuction.Domain.Enums;
using Shouldly;

namespace RealTimeAuction.Domain.UnitTests.Entities;

public class AuctionTests
{
    private static readonly DateTime Start = new(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = Start.AddHours(1);

    private static Auction OpenAuction(decimal currentPrice = 100m) => new()
    {
        Status = AuctionStatus.Active,
        StartTime = Start,
        EndTime = End,
        CurrentPrice = currentPrice,
    };

    [Test]
    public void IsActive_ShouldBeFalse_BeforeStartAndAfterEnd()
    {
        var auction = OpenAuction();

        auction.IsActive(Start.AddSeconds(-1)).ShouldBeFalse();
        auction.IsActive(End.AddSeconds(1)).ShouldBeFalse();
    }

    [Test]
    public void IsActive_ShouldBeFalse_WhenNotInActiveStatus()
    {
        var auction = OpenAuction();
        auction.Status = AuctionStatus.Scheduled;

        auction.IsActive(Start.AddMinutes(5)).ShouldBeFalse();
    }

    [Test]
    public void IsActive_ShouldBeTrue_InsideWindow()
    {
        OpenAuction().IsActive(Start.AddMinutes(30)).ShouldBeTrue();
    }

    [Test]
    public void CanPlaceBid_ShouldRequireAmountAboveCurrentPrice()
    {
        // CanPlaceBid usa o relógio real: a janela precisa estar aberta agora.
        var inWindow = OpenAuction(100m);
        inWindow.StartTime = DateTime.UtcNow.AddMinutes(-1);
        inWindow.EndTime = DateTime.UtcNow.AddHours(1);

        inWindow.CanPlaceBid(100m).ShouldBeFalse();
        inWindow.CanPlaceBid(99m).ShouldBeFalse();
        inWindow.CanPlaceBid(100.01m).ShouldBeTrue();
    }
}
