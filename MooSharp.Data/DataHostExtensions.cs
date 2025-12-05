using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MooSharp.Data.EntityFramework;

namespace MooSharp.Data;

public static class DataHostExtensions
{
    public static async Task EnsureMooSharpDatabaseCreatedAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MooSharpDbContext>>();

        await using var context = await factory.CreateDbContextAsync(cancellationToken);

        var connection = (SqliteConnection)context.Database.GetDbConnection();

        await connection.OpenAsync(cancellationToken);

        await context.Database.EnsureCreatedAsync(cancellationToken);

        // Enforce WAL Mode.
        // We run this OUTSIDE the 'if (created)' block. 
        // This ensures that even if the DB already exists, we force it to WAL mode.
        // This is very fast if it is already in WAL mode.
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
