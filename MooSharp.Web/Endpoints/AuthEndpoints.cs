using MooSharp.Data;
using MooSharp.Data.Mapping;
using MooSharp.Web.Services;

namespace MooSharp.Web.Endpoints;

public record RegisterRequest(string Username, string Password);

public record LoginRequest(string Username, string Password);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/register",
            async (RegisterRequest rc, World.World world, IPlayerRepository store, JwtTokenService tokenService) =>
            {
                var alreadyExists = await store.PlayerWithUsernameExistsAsync(rc.Username);

                if (alreadyExists)
                {
                    return Results.ValidationProblem([
                        new("Username", ["A player with that username already exists."])
                    ]);
                }

                var defaultRoom = world.GetDefaultRoom();

                var newPlayerRequest = PlayerSnapshotFactory.CreateNewPlayer(rc.Username, defaultRoom, rc.Password);

                await store.SaveNewPlayerAsync(newPlayerRequest, WriteType.Immediate);

                var token = tokenService.GenerateToken(newPlayerRequest.Username);

                return Results.Ok(new
                {
                    Token = token
                });
            });

        app.MapPost("/api/login",
            async (LoginRequest req, ILoginChecker checker, JwtTokenService tokenService) =>
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

                var token = tokenService.GenerateToken(req.Username);

                return Results.Ok(new
                {
                    Token = token
                });
            });
    }
}