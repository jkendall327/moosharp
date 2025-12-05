using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MooSharp.Data.EntityFramework;

internal sealed class SqliteWalInterceptor : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (connection is not SqliteConnection sqliteConnection)
        {
            return;
        }

        await using var pragma = sqliteConnection.CreateCommand();

        pragma.CommandText = "PRAGMA synchronous=NORMAL;";
        await pragma.ExecuteNonQueryAsync(cancellationToken);
    }
}
