using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MooSharp.Data.EntityFramework;
using MooSharp.Data.Queueing;

namespace MooSharp.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddMooSharpData(this IServiceCollection services, string databaseFilepath)
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

        services.AddSingleton(new DatabaseConfiguration(connectionString));

        services.AddDbContextFactory<MooSharpDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.AddInterceptors(new SqliteWalInterceptor());
        });

        var dbChannel = Channel.CreateUnbounded<DatabaseRequest>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

        services.AddSingleton(dbChannel.Writer);
        services.AddSingleton(dbChannel.Reader);

        services.AddSingleton<EfPlayerRepository>();
        services.AddSingleton<EfWorldRepository>();
        services.AddSingleton<IPlayerRepository, QueuedPlayerRepository>();
        services.AddSingleton<IWorldRepository, QueuedWorldRepository>();
        services.AddHostedService<DatabaseBackgroundService>();

        return services;
    }
}

public sealed record DatabaseConfiguration(string ConnectionString);
