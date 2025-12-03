using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MooSharp.Actors;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using MooSharp.Persistence.Dtos;

namespace MooSharp.Persistence;

public class SqlitePlayerStore : IPlayerStore
{
    private readonly string _connectionString;

    public SqlitePlayerStore(IOptions<AppOptions> options)
    {
        var databasePath = options.Value.DatabaseFilepath ??
                           throw new InvalidOperationException("DatabaseFilepath is not set.");

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        DapperTypeHandlerConfiguration.ConfigureRoomIdHandler();
        InitializeDatabase(databasePath);
    }

    public Task SaveNewPlayer(Player player,
        Room currentLocation,
        string password,
        CancellationToken ct = default)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var dto = PlayerSnapshotFactory.CreateNewPlayerDto(player, currentLocation, hashedPassword);

        return SaveNewPlayerSnapshotAsync(dto, ct);
    }

    public Task SavePlayer(Player player, Room currentLocation, CancellationToken ct = default)
    {
        var snapshot = PlayerSnapshotFactory.CreateSnapshot(player, currentLocation);

        return SavePlayerSnapshotAsync(snapshot, ct);
    }

    public async Task<PlayerDto?> LoadPlayer(LoginCommand command, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var player = await connection.QuerySingleOrDefaultAsync<PlayerDto>(
            "SELECT Username, Password, CurrentLocation FROM Players WHERE Username = @Username LIMIT 1",
            new
            {
                command.Username
            });

        if (player is null || !BCrypt.Net.BCrypt.Verify(command.Password, player.Password))
        {
            return null;
        }

        var inventory = await connection.QueryAsync<InventoryItemDto>("""
                                                                      SELECT ItemId as Id, Name, Description, TextContent, Flags, KeyId, CreatorUsername
                                                                      FROM PlayerInventory
                                                                      WHERE Username = @Username
                                                                      """,
            new
            {
                command.Username
            });

        player.Inventory = inventory.ToList();

        return player;
    }

    public async Task SaveNewPlayerSnapshotAsync(PlayerDto player, CancellationToken ct = default)
    {
        await UpsertPlayerAsync(player, ct);
        await ReplaceInventoryAsync(player.Username, player.Inventory, ct);
    }

    public async Task SavePlayerSnapshotAsync(PlayerSnapshotDto snapshot, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        await connection.OpenAsync(ct);

        await using var transaction = await connection.BeginTransactionAsync(ct);

        await connection.ExecuteAsync(
            "UPDATE Players SET CurrentLocation = @CurrentLocation WHERE Username = @Username",
            new
            {
                snapshot.Username,
                snapshot.CurrentLocation
            },
            transaction);

        await ReplaceInventoryAsync(connection, snapshot.Username, snapshot.Inventory, transaction);

        await transaction.CommitAsync(ct);
    }

    private async Task UpsertPlayerAsync(PlayerDto player, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
                           INSERT INTO Players (Username, Password, CurrentLocation)
                           VALUES (@Username, @Password, @CurrentLocation)
                           ON CONFLICT(Username) DO UPDATE SET
                               Password = excluded.Password,
                               CurrentLocation = excluded.CurrentLocation;
                           """;

        var command = new CommandDefinition(sql, player, cancellationToken: ct);

        await connection.ExecuteAsync(command);
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
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Cache = SqliteCacheMode.Shared
        }.ToString());

        connection.Open();

        connection.Execute("PRAGMA journal_mode=WAL;");
        connection.Execute("PRAGMA synchronous=NORMAL;");
        connection.Execute("PRAGMA foreign_keys=ON;");

        connection.Execute("""
                           CREATE TABLE IF NOT EXISTS Players
                           (
                               Username TEXT PRIMARY KEY,
                               Password TEXT NOT NULL,
                               CurrentLocation TEXT NOT NULL
                           );
                           """);

        connection.Execute("""
                           CREATE TABLE IF NOT EXISTS PlayerInventory
                           (
                               ItemId TEXT PRIMARY KEY,
                               Username TEXT NOT NULL,
                               Name TEXT NOT NULL,
                               Description TEXT NOT NULL,
                               TextContent TEXT,
                               Flags INTEGER NOT NULL DEFAULT 0,
                               KeyId TEXT,
                               CreatorUsername TEXT,
                               FOREIGN KEY (Username) REFERENCES Players (Username) ON DELETE CASCADE
                           );
                           """);

        connection.Execute("CREATE INDEX IF NOT EXISTS IX_PlayerInventory_Username ON PlayerInventory (Username);");

        EnsurePlayerInventoryColumns(connection);
    }

    private async Task ReplaceInventoryAsync(string username, IEnumerable<InventoryItemDto> inventory, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var transaction = await connection.BeginTransactionAsync(ct);

        await ReplaceInventoryAsync(connection, username, inventory, transaction);

        await transaction.CommitAsync(ct);
    }

    private static async Task ReplaceInventoryAsync(SqliteConnection connection,
        string username,
        IEnumerable<InventoryItemDto> inventory,
        IDbTransaction transaction)
    {
        const string deleteSql = "DELETE FROM PlayerInventory WHERE Username = @Username";

        const string insertSql = """
                                 INSERT INTO PlayerInventory (ItemId, Username, Name, Description, TextContent, Flags, KeyId, CreatorUsername)
                                 VALUES (@ItemId, @Username, @Name, @Description, @TextContent, @Flags, @KeyId, @CreatorUsername);
                                 """;

        await connection.ExecuteAsync(deleteSql,
            new
            {
                Username = username
            },
            transaction);

        if (!inventory.Any())
        {
            return;
        }

        var items = inventory.Select(o => new
        {
            ItemId = o.Id,
            Username = username,
            o.Name,
            o.Description,
            o.TextContent,
            Flags = (int) o.Flags,
            o.KeyId,
            o.CreatorUsername
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

        if (!existingColumns.Contains("CreatorUsername"))
        {
            connection.Execute("ALTER TABLE PlayerInventory ADD COLUMN CreatorUsername TEXT;");
        }
    }

    private sealed class TableInfo
    {
        public string Name { get; init; } = string.Empty;
    }
}