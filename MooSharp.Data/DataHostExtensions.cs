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
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
