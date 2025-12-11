using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MooSharp.Actors.Rooms;
using MooSharp.Data.EntityFramework;
using MooSharp.World;

namespace MooSharp.Tests.IntegrationTests;

public class MooSharpWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove background services that aren't needed for auth tests
            services.RemoveAll<IHostedService>();

            // Remove the existing DbContextFactory registration
            services.RemoveAll<IDbContextFactory<MooSharpDbContext>>();
            services.RemoveAll<DbContextOptions<MooSharpDbContext>>();

            // Create and open a persistent in-memory SQLite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add the test DbContextFactory
            services.AddDbContextFactory<MooSharpDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Replace the WorldSeeder with a test seeder that doesn't require a file
            services.RemoveAll<IWorldSeeder>();
            services.AddSingleton<IWorldSeeder, TestWorldSeeder>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Ensure database is created and World is initialized
        using var scope = host.Services.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MooSharpDbContext>>();
        using var context = factory.CreateDbContext();
        context.Database.EnsureCreated();

        var worldInitializer = scope.ServiceProvider.GetRequiredService<WorldInitializer>();
        worldInitializer.InitializeAsync().GetAwaiter().GetResult();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}

public class TestWorldSeeder : IWorldSeeder
{
    public IReadOnlyCollection<Room> GetSeedRooms()
    {
        return
        [
            new Room
            {
                Id = "start",
                Name = "Starting Room",
                Description = "A test starting room.",
                LongDescription = "This is a test starting room for integration tests.",
                CreatorUsername = "system"
            }
        ];
    }
}
