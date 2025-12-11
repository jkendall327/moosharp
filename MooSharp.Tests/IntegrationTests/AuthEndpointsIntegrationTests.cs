using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MooSharp.Web.Endpoints;

namespace MooSharp.Tests.IntegrationTests;

public class AuthEndpointsIntegrationTests : IClassFixture<MooSharpWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsIntegrationTests(MooSharpWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new RegisterRequest("testuser", "password123");

        // Act
        var response = await _client.PostAsJsonAsync(AuthEndpoints.RegistrationEndpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RegisterResult>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);

        // Verify the token is a valid JWT
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(result.Token));

        var token = handler.ReadJwtToken(result.Token);
        Assert.Equal("testuser", token.Claims.First(c => c.Type == "name").Value);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsValidationProblem()
    {
        // Arrange - Register the first user
        var request = new RegisterRequest("duplicateuser", "password123");
        await _client.PostAsJsonAsync(AuthEndpoints.RegistrationEndpoint, request);

        // Act - Try to register with the same username
        var response = await _client.PostAsJsonAsync(AuthEndpoints.RegistrationEndpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Username"));
        Assert.Contains("already exists", problem.Errors["Username"].First());
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange - First register a user
        var username = "loginuser";
        var password = "password123";
        var registerRequest = new RegisterRequest(username, password);
        await _client.PostAsJsonAsync(AuthEndpoints.RegistrationEndpoint, registerRequest);

        // Act - Login with the same credentials
        var loginRequest = new LoginRequest(username, password);
        var response = await _client.PostAsJsonAsync(AuthEndpoints.LoginEndpoint, loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginAttemptResult>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);

        // Verify the token is a valid JWT with correct claims
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(result.Token));

        var token = handler.ReadJwtToken(result.Token);
        Assert.Equal(username, token.Claims.First(c => c.Type == "name").Value);
    }

    [Fact]
    public async Task Login_WithNonExistentUsername_ReturnsValidationProblem()
    {
        // Arrange
        var request = new LoginRequest("nonexistentuser", "password123");

        // Act
        var response = await _client.PostAsJsonAsync(AuthEndpoints.LoginEndpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Username"));
        Assert.Contains("doesn't exist", problem.Errors["Username"].First());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange - First register a user
        var username = "wrongpassworduser";
        var registerRequest = new RegisterRequest(username, "correctpassword");
        await _client.PostAsJsonAsync(AuthEndpoints.RegistrationEndpoint, registerRequest);

        // Act - Login with wrong password
        var loginRequest = new LoginRequest(username, "wrongpassword");
        var response = await _client.PostAsJsonAsync(AuthEndpoints.LoginEndpoint, loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_TokenContainsCorrectClaims()
    {
        // Arrange
        var username = "claimsuser";
        var request = new RegisterRequest(username, "password123");

        // Act
        var response = await _client.PostAsJsonAsync(AuthEndpoints.RegistrationEndpoint, request);
        var result = await response.Content.ReadFromJsonAsync<RegisterResult>();

        // Assert
        Assert.NotNull(result);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);

        // Verify required claims exist
        Assert.Contains(token.Claims, c => c.Type == "sub"); // Subject (user ID)
        Assert.Contains(token.Claims, c => c.Type == "name" && c.Value == username);
        Assert.Contains(token.Claims, c => c.Type == "jti"); // JWT ID
        Assert.Equal("MooSharpGame", token.Issuer);
        Assert.Contains("MooSharpClient", token.Audiences);
    }

    [Fact]
    public async Task Login_AfterRegister_TokenContainsSameUserId()
    {
        // Arrange - Register a user
        var username = "sameiduser";
        var password = "password123";
        var registerRequest = new RegisterRequest(username, password);
        var registerResponse = await _client.PostAsJsonAsync(AuthEndpoints.RegistrationEndpoint, registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResult>();

        // Act - Login
        var loginRequest = new LoginRequest(username, password);
        var loginResponse = await _client.PostAsJsonAsync(AuthEndpoints.LoginEndpoint, loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginAttemptResult>();

        // Assert - Both tokens should have the same user ID in the 'sub' claim
        Assert.NotNull(registerResult);
        Assert.NotNull(loginResult);

        var handler = new JwtSecurityTokenHandler();
        var registerToken = handler.ReadJwtToken(registerResult.Token);
        var loginToken = handler.ReadJwtToken(loginResult.Token);

        var registerSub = registerToken.Claims.First(c => c.Type == "sub").Value;
        var loginSub = loginToken.Claims.First(c => c.Type == "sub").Value;

        Assert.Equal(registerSub, loginSub);
    }
}
