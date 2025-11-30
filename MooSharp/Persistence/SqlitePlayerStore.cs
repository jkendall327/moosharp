using System;
using System.Data;
using BCrypt.Net;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MooSharp.Infrastructure;

namespace MooSharp.Persistence;

public class SqlitePlayerStore : IPlayerStore
{
    private readonly string _connectionString;
    public SqlitePlayerStore(IOptions<AppOptions> options)
    {
        var databasePath = options.Value.DatabaseFilepath
            ?? throw new InvalidOperationException("DatabaseFilepath is not set.");

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        DapperTypeHandlerConfiguration.ConfigureRoomIdHandler();
        InitializeDatabase(databasePath);
    }

    public async Task SaveNewPlayer(Player player, Room currentLocation, string password, CancellationToken ct = default)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var dto = new PlayerDto
        {
            Username = player.Username,
            Password = hashedPassword,
            CurrentLocation = currentLocation.Id
        };

        await UpsertPlayerAsync(dto);
        await ReplaceInventoryAsync(player);
    }

    public async Task SavePlayer(Player player, Room currentLocation, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        await connection.OpenAsync(ct);

        await using var transaction = await connection.BeginTransactionAsync(ct);

        await connection.ExecuteAsync(
            "UPDATE Players SET CurrentLocation = @CurrentLocation WHERE Username = @Username",
            new { player.Username, CurrentLocation = currentLocation.Id },
            transaction);

        await ReplaceInventoryAsync(connection, player, transaction);

        await transaction.CommitAsync(ct);
    }

    public async Task<PlayerDto?> LoadPlayer(LoginCommand command, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var player = await connection.QuerySingleOrDefaultAsync<PlayerDto>(
            "SELECT Username, Password, CurrentLocation FROM Players WHERE Username = @Username LIMIT 1",
            new { command.Username });

        if (player is null || !BCrypt.Net.BCrypt.Verify(command.Password, player.Password))
        {
            return null;
        }

        var inventory = await connection.QueryAsync<InventoryItemDto>(
            """
            SELECT ItemId as Id, Name, Description, TextContent, Flags, KeyId
            FROM PlayerInventory
            WHERE Username = @Username
            """,
            new { command.Username });

        player.Inventory = inventory.ToList();

        return player;
    }

    private async Task UpsertPlayerAsync(PlayerDto player)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            INSERT INTO Players (Username, Password, CurrentLocation)
            VALUES (@Username, @Password, @CurrentLocation)
            ON CONFLICT(Username) DO UPDATE SET
                Password = excluded.Password,
                CurrentLocation = excluded.CurrentLocation;
            """;

        await connection.ExecuteAsync(sql, player);
    }

    private static void InitializeDatabase(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());

        connection.Open();

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS Players
            (
                Username TEXT PRIMARY KEY,
                Password TEXT NOT NULL,
                CurrentLocation TEXT NOT NULL
            );
            """);

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS PlayerInventory
            (
                ItemId TEXT PRIMARY KEY,
                Username TEXT NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                TextContent TEXT,
                Flags INTEGER NOT NULL DEFAULT 0,
                KeyId TEXT,
                FOREIGN KEY (Username) REFERENCES Players (Username) ON DELETE CASCADE
            );
            """);

        connection.Execute("CREATE INDEX IF NOT EXISTS IX_PlayerInventory_Username ON PlayerInventory (Username);");

        EnsurePlayerInventoryColumns(connection);
    }

    private async Task ReplaceInventoryAsync(Player player)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        await ReplaceInventoryAsync(connection, player, transaction);

        await transaction.CommitAsync();
    }

    private static async Task ReplaceInventoryAsync(SqliteConnection connection, Player player, IDbTransaction transaction)
    {
        const string deleteSql = "DELETE FROM PlayerInventory WHERE Username = @Username";
        const string insertSql =
            """
            INSERT INTO PlayerInventory (ItemId, Username, Name, Description, TextContent, Flags, KeyId)
            VALUES (@ItemId, @Username, @Name, @Description, @TextContent, @Flags, @KeyId);
            """;

        await connection.ExecuteAsync(deleteSql, new { player.Username }, transaction);

        if (!player.Inventory.Any())
        {
            return;
        }

        var items = player.Inventory
            .Select(o => new
            {
                ItemId = o.Id.Value.ToString(),
                player.Username,
                o.Name,
                o.Description,
                o.TextContent,
                Flags = (int)o.Flags,
                o.KeyId
            });

        await connection.ExecuteAsync(insertSql, items, transaction);
    }

    private static void EnsurePlayerInventoryColumns(SqliteConnection connection)
    {
        var existingColumns = connection
            .Query<TableInfo>("PRAGMA table_info('PlayerInventory');")
            .Select(c => c.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingColumns.Contains("Flags"))
        {
            connection.Execute("ALTER TABLE PlayerInventory ADD COLUMN Flags INTEGER NOT NULL DEFAULT 0;");
        }

        if (!existingColumns.Contains("KeyId"))
        {
            connection.Execute("ALTER TABLE PlayerInventory ADD COLUMN KeyId TEXT;");
        }
    }

    private sealed class TableInfo
    {
        public string Name { get; init; } = string.Empty;
    }
}
