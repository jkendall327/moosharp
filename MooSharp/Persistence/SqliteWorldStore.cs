using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MooSharp;

namespace MooSharp.Persistence;

public class SqliteWorldStore : IWorldStore
{
    private readonly string _connectionString;

    public SqliteWorldStore(IOptions<AppOptions> options)
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

    public async Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var count = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM Rooms");

        return count > 0;
    }

    public async Task<IReadOnlyCollection<Room>> LoadRoomsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var rooms = await connection.QueryAsync<RoomRecord>(
            "SELECT Id, Name, Description, LongDescription, EnterText, ExitText FROM Rooms");

        var roomDictionary = rooms.ToDictionary(
            r => r.Id,
            r => new Room
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                LongDescription = r.LongDescription,
                EnterText = r.EnterText,
                ExitText = r.ExitText
            });

        var exits = await connection.QueryAsync<ExitRecord>(
            "SELECT FromRoomId, Direction, ToRoomId FROM Exits");

        foreach (var exit in exits)
        {
            if (!roomDictionary.TryGetValue(exit.FromRoomId, out var fromRoom))
            {
                continue;
            }

            if (!roomDictionary.ContainsKey(exit.ToRoomId))
            {
                continue;
            }

            fromRoom.Exits[exit.Direction] = exit.ToRoomId;
        }

        return roomDictionary.Values.ToList();
    }

    public async Task SaveRoomAsync(Room room, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            INSERT INTO Rooms (Id, Name, Description, LongDescription, EnterText, ExitText)
            VALUES (@Id, @Name, @Description, @LongDescription, @EnterText, @ExitText)
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name,
                Description = excluded.Description,
                LongDescription = excluded.LongDescription,
                EnterText = excluded.EnterText,
                ExitText = excluded.ExitText;
            """;

        await connection.ExecuteAsync(sql, room);
    }

    public async Task SaveExitAsync(RoomId fromRoomId, RoomId toRoomId, string direction, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            INSERT INTO Exits (FromRoomId, Direction, ToRoomId)
            VALUES (@FromRoomId, @Direction, @ToRoomId)
            ON CONFLICT(FromRoomId, Direction) DO UPDATE SET
                ToRoomId = excluded.ToRoomId;
            """;

        await connection.ExecuteAsync(sql, new { FromRoomId = fromRoomId, Direction = direction, ToRoomId = toRoomId });
    }

    public async Task SaveRoomsAsync(IEnumerable<Room> rooms, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string insertRoomSql = """
            INSERT INTO Rooms (Id, Name, Description, LongDescription, EnterText, ExitText)
            VALUES (@Id, @Name, @Description, @LongDescription, @EnterText, @ExitText)
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name,
                Description = excluded.Description,
                LongDescription = excluded.LongDescription,
                EnterText = excluded.EnterText,
                ExitText = excluded.ExitText;
            """;

        const string insertExitSql = """
            INSERT INTO Exits (FromRoomId, Direction, ToRoomId)
            VALUES (@FromRoomId, @Direction, @ToRoomId)
            ON CONFLICT(FromRoomId, Direction) DO UPDATE SET
                ToRoomId = excluded.ToRoomId;
            """;

        foreach (var room in rooms)
        {
            await connection.ExecuteAsync(insertRoomSql, room, transaction);

            var exits = room.Exits.Select(exit => new
            {
                FromRoomId = room.Id,
                Direction = exit.Key,
                ToRoomId = exit.Value
            });

            if (exits.Any())
            {
                await connection.ExecuteAsync(insertExitSql, exits, transaction);
            }
        }

        await transaction.CommitAsync(cancellationToken);
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
            CREATE TABLE IF NOT EXISTS Rooms
            (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                LongDescription TEXT NOT NULL,
                EnterText TEXT NOT NULL,
                ExitText TEXT NOT NULL
            );
            """);

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS Exits
            (
                FromRoomId TEXT NOT NULL,
                Direction TEXT NOT NULL,
                ToRoomId TEXT NOT NULL,
                PRIMARY KEY (FromRoomId, Direction),
                FOREIGN KEY (FromRoomId) REFERENCES Rooms(Id),
                FOREIGN KEY (ToRoomId) REFERENCES Rooms(Id)
            );
            """);
    }

    private record RoomRecord(RoomId Id, string Name, string Description, string LongDescription, string EnterText, string ExitText);

    private record ExitRecord(RoomId FromRoomId, string Direction, RoomId ToRoomId);
}
