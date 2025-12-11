using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MooSharp.Web.Endpoints;

namespace MooSharp.Tests.Endpoints;

public class AuthEndpointsTests : IAsyncLifetime
{
    private readonly string _databaseDirectory;
    private readonly string _databasePath;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests()
    {
        _databaseDirectory = Path.Combine(Path.GetTempPath(), "moosharp-tests", Guid.NewGuid().ToString("N"));
        _databasePath = Path.Combine(_databaseDirectory, "auth.db");

        var worldDataPath = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "MooSharp.Web", "world.json"));

        _factory = new AuthWebApplicationFactory(_databasePath, worldDataPath);
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RegisterAndLoginEndpoints_ReturnTokens()
    {
        var username = $"integration-user-{Guid.NewGuid():N}";
        const string password = "integration-password";

        var registerResponse = await _client.PostAsJsonAsync(
            AuthEndpoints.RegistrationEndpoint,
            new RegisterRequest(username, password));

        registerResponse.EnsureSuccessStatusCode();

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResult>();

        Assert.NotNull(registerResult);
        Assert.False(string.IsNullOrWhiteSpace(registerResult!.Token));

        var loginResponse = await _client.PostAsJsonAsync(AuthEndpoints.LoginEndpoint, new LoginRequest(username, password));

        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginAttemptResult>();

        Assert.NotNull(loginResult);
        Assert.False(string.IsNullOrWhiteSpace(loginResult!.Token));
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();

        if (Directory.Exists(_databaseDirectory))
        {
            Directory.Delete(_databaseDirectory, true);
        }

        return Task.CompletedTask;
    }

    private sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath;
        private readonly string _worldDataPath;

        public AuthWebApplicationFactory(string databasePath, string worldDataPath)
        {
            _databasePath = databasePath;
            _worldDataPath = worldDataPath;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["AppOptions:DatabaseFilepath"] = _databasePath,
                    ["AppOptions:WorldDataFilepath"] = _worldDataPath
                };

                configurationBuilder.AddInMemoryCollection(settings);
            });
        }
    }
}
