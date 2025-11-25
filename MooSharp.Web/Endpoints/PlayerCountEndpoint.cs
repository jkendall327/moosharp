using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MooSharp;

namespace MooSharp.Web.Endpoints;

public static class PlayerCountEndpoint
{
    public static IEndpointRouteBuilder MapPlayerCountEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/player-count", (World world) => Results.Ok(new PlayerCountResponse(world.Players.Count)))
            .WithName("GetPlayerCount");

        return app;
    }

    private sealed record PlayerCountResponse(int Count);
}
