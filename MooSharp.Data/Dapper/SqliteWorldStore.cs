using Dapper;
using Microsoft.Data.Sqlite;
using MooSharp.Data.Dtos;

namespace MooSharp.Data.Dapper;

public sealed class SqliteWorldStore : IWorldStore
{
    private readonly string _connectionString;

    public SqliteWorldStore(DatabaseConfiguration configuration)
    {
        _connectionString = configuration.ConnectionString;
        InitializeDatabase();
    }

    public async Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var count = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM Rooms");

        return count > 0;
    }

    public async Task<IReadOnlyCollection<RoomSnapshotDto>> LoadRoomsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var rooms = await connection.QueryAsync<RoomRecord>(
            "SELECT Id, Name, Description, LongDescription, EnterText, ExitText, CreatorUsername FROM Rooms");

        var exits = await connection.QueryAsync<ExitRecord>("SELECT FromRoomId, ToRoomId FROM Exits");
        var objects = await connection.QueryAsync<ObjectRecord>(
            "SELECT Id, RoomId, Name, Description, TextContent, Flags, KeyId, CreatorUsername FROM Objects");

        var roomDictionary = rooms.ToDictionary(
            r => r.Id,
            r => new RoomSnapshotDto(
                r.Id,
                r.Name,
                r.Description,
                r.LongDescription,
                r.EnterText,
                r.ExitText,
                r.CreatorUsername,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                Array.Empty<ObjectSnapshotDto>()));

        foreach (var exit in exits)
        {
            if (roomDictionary.TryGetValue(exit.FromRoomId, out var fromRoom) && roomDictionary.ContainsKey(exit.ToRoomId))
            {
                ((Dictionary<string, string>)fromRoom.Exits)[exit.ToRoomId] = exit.ToRoomId;
            }
        }

        foreach (var obj in objects)
        {
            if (roomDictionary.TryGetValue(obj.RoomId, out var room))
            {
                var list = room.Objects.ToList();
                list.Add(new ObjectSnapshotDto(
                    obj.Id,
                    obj.RoomId,
                    obj.Name,
                    obj.Description,
                    obj.TextContent,
                    obj.Flags,
                    obj.KeyId,
                    obj.CreatorUsername));
                roomDictionary[obj.RoomId] = room with { Objects = list };
            }
        }

        return roomDictionary.Values.ToList();
    }

    public async Task SaveRoomAsync(RoomSnapshotDto room, CancellationToken cancellationToken = default)
    {
        await SaveRoomSnapshotsAsync([room], cancellationToken);
    }

    public async Task SaveExitAsync(string fromRoomId, string toRoomId, string direction, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            INSERT INTO Exits (FromRoomId, ToRoomId)
            VALUES (@FromRoomId, @ToRoomId)
            ON CONFLICT(FromRoomId, ToRoomId) DO NOTHING;
            """;

        await connection.ExecuteAsync(sql, new { FromRoomId = fromRoomId, ToRoomId = toRoomId });
    }

    public async Task SaveRoomsAsync(IEnumerable<RoomSnapshotDto> rooms, CancellationToken cancellationToken = default)
    {
        await SaveRoomSnapshotsAsync(rooms, cancellationToken);
    }

    public async Task UpdateRoomDescriptionAsync(string roomId, string description, string longDescription,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            UPDATE Rooms
            SET Description = @Description,
                LongDescription = @LongDescription
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(sql, new { Id = roomId, Description = description, LongDescription = longDescription });
    }

    public async Task RenameRoomAsync(string roomId, string name, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            UPDATE Rooms
            SET Name = @Name
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(sql, new { Id = roomId, Name = name });
    }

    public async Task RenameObjectAsync(string objectId, string name, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            UPDATE Objects
            SET Name = @Name
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(sql, new { Id = objectId, Name = name });
    }

    private async Task SaveRoomSnapshotsAsync(IEnumerable<RoomSnapshotDto> rooms, CancellationToken cancellationToken)
    {
        var roomList = rooms.ToList();

        if (roomList.Count == 0)
        {
            return;
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string insertRoomSql = """
            INSERT INTO Rooms (Id, Name, Description, LongDescription, EnterText, ExitText, CreatorUsername)
            VALUES (@Id, @Name, @Description, @LongDescription, @EnterText, @ExitText, @CreatorUsername)
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name,
                Description = excluded.Description,
                LongDescription = excluded.LongDescription,
                EnterText = excluded.EnterText,
                ExitText = excluded.ExitText,
                CreatorUsername = excluded.CreatorUsername;
            """;

        const string insertExitSql = """
            INSERT INTO Exits (FromRoomId, ToRoomId)
            VALUES (@FromRoomId, @ToRoomId)
            ON CONFLICT(FromRoomId, ToRoomId) DO NOTHING;
            """;

        const string deleteObjectsSql = "DELETE FROM Objects WHERE RoomId = @RoomId;";

        const string insertObjectSql = """
            INSERT INTO Objects (Id, RoomId, Name, Description, TextContent, Flags, KeyId, CreatorUsername)
            VALUES (@Id, @RoomId, @Name, @Description, @TextContent, @Flags, @KeyId, @CreatorUsername)
            ON CONFLICT(Id) DO UPDATE SET
                RoomId = excluded.RoomId,
                Name = excluded.Name,
                Description = excluded.Description,
                TextContent = excluded.TextContent,
                Flags = excluded.Flags,
                KeyId = excluded.KeyId,
                CreatorUsername = excluded.CreatorUsername;
            """;

        foreach (var room in roomList)
        {
            await connection.ExecuteAsync(insertRoomSql, room, transaction);
        }

        foreach (var room in roomList)
        {
            var exits = room.Exits.Select(exit => new
            {
                FromRoomId = room.Id,
                ToRoomId = exit.Value
            });

            if (exits.Any())
            {
                await connection.ExecuteAsync(insertExitSql, exits, transaction);
            }
        }

        foreach (var room in roomList)
        {
            await connection.ExecuteAsync(deleteObjectsSql, new { RoomId = room.Id }, transaction);

            var objects = room.Objects.Select(o => new
            {
                Id = o.Id,
                RoomId = room.Id,
                o.Name,
                o.Description,
                o.TextContent,
                o.Flags,
                o.KeyId,
                o.CreatorUsername
            });

            if (objects.Any())
            {
                await connection.ExecuteAsync(insertObjectSql, objects, transaction);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);

        connection.Open();

        connection.Execute("PRAGMA journal_mode=WAL;");
        connection.Execute("PRAGMA synchronous=NORMAL;");
        connection.Execute("PRAGMA foreign_keys=ON;");

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS Rooms
            (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                LongDescription TEXT NOT NULL,
                EnterText TEXT NOT NULL,
                ExitText TEXT NOT NULL,
                CreatorUsername TEXT
            );
            """);

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS Exits
            (
                FromRoomId TEXT NOT NULL,
                ToRoomId TEXT NOT NULL,
                PRIMARY KEY (FromRoomId, ToRoomId),
                FOREIGN KEY (FromRoomId) REFERENCES Rooms(Id) ON DELETE CASCADE,
                FOREIGN KEY (ToRoomId) REFERENCES Rooms(Id) ON DELETE CASCADE
            );
            """);

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS Objects
            (
                Id TEXT PRIMARY KEY,
                RoomId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                TextContent TEXT,
                Flags INTEGER NOT NULL DEFAULT 0,
                KeyId TEXT,
                CreatorUsername TEXT,
                FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE
            );
            """);

        connection.Execute("CREATE INDEX IF NOT EXISTS IX_Objects_RoomId ON Objects (RoomId);");
    }

    private sealed record RoomRecord(string Id, string Name, string Description, string LongDescription, string EnterText, string ExitText, string? CreatorUsername);
    private sealed record ExitRecord(string FromRoomId, string ToRoomId);

    private sealed class ObjectRecord
    {
        public string Id { get; init; } = string.Empty;
        public string RoomId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string? TextContent { get; init; }
        public int Flags { get; init; }
        public string? KeyId { get; init; }
        public string? CreatorUsername { get; init; }
    }
}
