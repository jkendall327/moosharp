using Microsoft.AspNetCore.Mvc;
using MooSharp.Data;
using MooSharp.Data.Dtos;
using MooSharp.Data.Mapping;
using MooSharp.Web.Services;

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
                var alreadyExists = await store.PlayerWithUsernameExistsAsync(rc.Username);

                if (alreadyExists)
                {
                    return Results.ValidationProblem([
                        new("Username", ["A player with that username already exists."])
                    ]);
                }

                var defaultRoom = world.GetDefaultRoom();

                var request = new NewPlayerRequest(rc.Username, rc.Password, defaultRoom.Id.Value);

                await store.SaveNewPlayerAsync(request, WriteType.Immediate);

                var token = tokenService.GenerateToken(rc.Username);

                return Results.Ok(new RegisterResult(token));
            });

        app.MapPost(LoginEndpoint,
            async (LoginRequest req,
                [FromServices] ILoginChecker checker,
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

                var token = tokenService.GenerateToken(req.Username);

                return Results.Ok(new LoginAttemptResult(token));
            });
    }
}