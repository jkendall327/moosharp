using Microsoft.AspNetCore.Mvc;
using MooSharp.Data;
using MooSharp.Data.Players;
using MooSharp.Web.Services.Auth;

namespace MooSharp.Web.Endpoints;

public record RegisterRequest(string Username, string Password);

public record RegisterResult(string Token);

public record LoginRequest(string Username, string Password);

public record LoginAttemptResult(string Token);

public static class AuthEndpoints
{
    public const string RegistrationEndpoint = "/api/register";
    public const string LoginEndpoint = "/api/login";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(RegistrationEndpoint,
            async (RegisterRequest rc,
                [FromServices] World.World world,
                [FromServices] IPlayerRepository store,
                [FromServices] JwtTokenService tokenService) =>
            {
                var player = await store.GetPlayerByUsername(rc.Username);

                if (player is not null)
                {
                    return Results.ValidationProblem([
                        new("Username", ["A player with that username already exists."])
                    ]);
                }

                var defaultRoom = world.GetDefaultRoom();

                var id = Guid.NewGuid();

                var request = new NewPlayerRequest(id, rc.Username, rc.Password, defaultRoom.Id.Value);

                await store.SaveNewPlayerAsync(request, WriteType.Immediate);

                var token = tokenService.GenerateToken(id, rc.Username);

                return Results.Ok(new RegisterResult(token));
            });

        app.MapPost(LoginEndpoint,
            async (LoginRequest req,
                [FromServices] ILoginChecker checker,
                [FromServices] IPlayerRepository playerRepository,
                [FromServices] JwtTokenService tokenService) =>
            {
                var result = await checker.LoginIsValidAsync(req.Username, req.Password);

                if (result is LoginResult.UsernameNotFound)
                {
                    return Results.ValidationProblem([new("Username", ["That username doesn't exist."])]);
                }

                if (result is LoginResult.WrongPassword)
                {
                    return Results.Unauthorized();
                }

                if (result is not LoginResult.Ok)
                {
                    throw new InvalidOperationException("Unknown login result.");
                }

                var player = await playerRepository.GetPlayerByUsername(req.Username);

                if (player is null)
                {
                    throw new InvalidOperationException("Player not found despite login passing validity check.");
                }

                var token = tokenService.GenerateToken(player.Id, req.Username);

                return Results.Ok(new LoginAttemptResult(token));
            });
    }
}