using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;
using RealTimeAuction.Application.Auctions.Commands.GetAuctionById;
using RealTimeAuction.Domain.Enums;

namespace RealTimeAuction.Infrastructure.IntegrationTests;

[TestFixture]
public class AuctionApiTests : IntegrationTestBase
{
    [Test]
    public async Task Validation_ShouldPrevent_SellerFromBiddingOnOwnAuction()
    {
        var createAuctionCommand = new
        {
            Title = "Leilão Teste Integração",
            Description = "Descrição do leilão de teste",
            StartingPrice = 100m,
            StartTime = DateTime.UtcNow.AddMinutes(1),
            EndTime = DateTime.UtcNow.AddHours(1),
        };

        var createResponse = await Client.PostAsJsonAsync("/api/auctions", createAuctionCommand);
        createResponse.EnsureSuccessStatusCode();

        var auctionData = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        int auctionId = auctionData.GetProperty("id").GetInt32();

        var placeBidCommand = new { Amount = 150m };
        var bidResponse = await Client.PostAsJsonAsync(
            $"/api/auctions/{auctionId}/bids",
            placeBidCommand
        );

        Assert.That(
            bidResponse.IsSuccessStatusCode,
            Is.False,
            "O vendedor não deveria conseguir dar lance no próprio leilão"
        );
    }

    [Test]
    public async Task CreateAuction_Should_SaveToDatabase()
    {
        var command = new
        {
            Title = "Leilão Válido",
            Description = "Integração PostgreSQL",
            StartingPrice = 500m,
            StartTime = DateTime.UtcNow.AddMinutes(5),
            EndTime = DateTime.UtcNow.AddDays(1),
        };

        var response = await Client.PostAsJsonAsync("/api/auctions", command);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        int id = result.GetProperty("id").GetInt32();

        Assert.That(id, Is.GreaterThan(0));

        var getResponse = await Client.GetAsync($"/api/auctions/{id}");
        getResponse.EnsureSuccessStatusCode();

        var auction = await getResponse.Content.ReadFromJsonAsync<AuctionDetailDto>();
        Assert.That(auction, Is.Not.Null);
        Assert.That(auction.Title, Is.EqualTo("Leilão Válido"));
        Assert.That(auction.Status, Is.EqualTo(AuctionStatus.Scheduled));
    }
}
