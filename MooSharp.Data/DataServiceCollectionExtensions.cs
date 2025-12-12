using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MooSharp.Data.EntityFramework;
using MooSharp.Data.Players;
using MooSharp.Data.Worlds;

namespace MooSharp.Data;

public static class DataServiceCollectionExtensions
{
    public static void AddMooSharpData(this IServiceCollection services, string databaseFilepath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseFilepath);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseFilepath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        var directory = Path.GetDirectoryName(databaseFilepath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        services.AddDbContextFactory<MooSharpDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(new SqliteWalInterceptor());
            options.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
        });

        var dbChannel = Channel.CreateUnbounded<DatabaseRequest>(new()
        {
            SingleReader = true
        });

        services.AddSingleton(dbChannel.Writer);
        services.AddSingleton(dbChannel.Reader);

        services.AddSingleton<EfWorldRepository>();
        services.AddSingleton<IWorldRepository>(sp => sp.GetRequiredService<EfWorldRepository>());
        services.AddSingleton<IPlayerRepository, EfPlayerRepository>();
        services.AddSingleton<IPlayerStore, EfPlayerStore>();
        services.AddSingleton<ILoginChecker, EfPlayerStore>();
        services.AddHostedService<DatabaseBackgroundService>();
    }

    public static async Task EnsureMooSharpDatabaseCreatedAsync(this IHost host,
        CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MooSharpDbContext>>();

        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var connection = (SqliteConnection)context.Database.GetDbConnection();

        await connection.OpenAsync(cancellationToken);

        await context.Database.EnsureCreatedAsync(cancellationToken);

        // Enforce WAL Mode.
        // This is very fast if it is already in WAL mode.
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}