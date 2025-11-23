using System;
using System.Data;
using BCrypt.Net;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace MooSharp.Persistence;

public class SqlitePlayerStore : IPlayerStore
{
    private readonly string _connectionString;
    private static bool _roomIdHandlerConfigured;

    public SqlitePlayerStore(IOptions<AppOptions> options)
    {
        var databasePath = options.Value.PlayerDatabaseFilepath
            ?? throw new InvalidOperationException("PlayerDatabaseFilepath is not set.");

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        ConfigureTypeHandlers();
        InitializeDatabase(databasePath);
    }

    public async Task SaveNewPlayer(Player player, Room currentLocation, string password)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var dto = new PlayerDto
        {
            Username = player.Username,
            Password = hashedPassword,
            CurrentLocation = currentLocation.Id
        };

        await UpsertPlayerAsync(dto);
    }

    public async Task SavePlayer(Player player, Room currentLocation)
    {
        await using var connection = new SqliteConnection(_connectionString);

        await connection.ExecuteAsync(
            "UPDATE Players SET CurrentLocation = @CurrentLocation WHERE Username = @Username",
            new { player.Username, CurrentLocation = currentLocation.Id });
    }

    public async Task<PlayerDto?> LoadPlayer(LoginCommand command)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var player = await connection.QuerySingleOrDefaultAsync<PlayerDto>(
            "SELECT Username, Password, CurrentLocation FROM Players WHERE Username = @Username LIMIT 1",
            new { command.Username });

        return player is not null && BCrypt.Net.BCrypt.Verify(command.Password, player.Password)
            ? player
            : null;
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

    private static void ConfigureTypeHandlers()
    {
        if (_roomIdHandlerConfigured)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new RoomIdTypeHandler());
        _roomIdHandlerConfigured = true;
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
    }

    private class RoomIdTypeHandler : SqlMapper.TypeHandler<RoomId>
    {
        public override RoomId Parse(object value) => new(Convert.ToString(value) ?? string.Empty);

        public override void SetValue(IDbDataParameter parameter, RoomId value)
        {
            parameter.Value = value.Value;
        }
    }
}
