using MediatR;
using RealTimeAuction.Application.Auctions.Commands.CreateAuction;
using RealTimeAuction.Application.Auctions.Commands.GetAuctionById;
using RealTimeAuction.Application.Auctions.Commands.GetAuctions;
using RealTimeAuction.Application.Auctions.Commands.PlaceBid;
using RealTimeAuction.Application.Common.Models;
using RealTimeAuction.Web.Infrastructure.Idempotency;

namespace RealTimeAuction.Web.Endpoints;

public class Auctions : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetAuctions);
        group.MapGet("/{id}", GetAuctionById);
        group.MapPost("/", CreateAuction).RequireAuthorization();
        group.MapPost("/{id}/bids", PlaceBid)
            .RequireAuthorization()
            .AddEndpointFilter<IdempotencyFilter>();
    }

    public async Task<PaginatedList<AuctionDto>> GetAuctions(
        ISender sender,
        [AsParameters] GetAuctionsQuery query
    )
    {
        return await sender.Send(query);
    }

    public async Task<AuctionDetailDto> GetAuctionById(ISender sender, int id)
    {
        return await sender.Send(new GetAuctionByIdQuery { Id = id });
    }

    public async Task<IResult> CreateAuction(ISender sender, CreateAuctionCommand command)
    {
        var id = await sender.Send(command);
        return Results.Created($"/api/auctions/{id}", new { Id = id });
    }

    public async Task<IResult> PlaceBid(ISender sender, int id, PlaceBidCommand command)
    {
        var result = await sender.Send(command with { AuctionId = id });
        return Results.Created($"/api/auctions/{id}/bids/{result.Id}", result);
    }
}
