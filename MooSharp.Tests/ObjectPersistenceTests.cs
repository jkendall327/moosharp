using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MooSharp.Actors;
using MooSharp.Data;
using MooSharp.Data.Dapper;
using MooSharp.Data.Dtos;
using MooSharp.Data.Mapping;
using MooSharp.Infrastructure;
using MooSharp.Tests.Handlers;
using Object = MooSharp.Actors.Object;

namespace MooSharp.Tests;

public class SqlitePlayerStoreObjectPersistenceTests
{
    [Fact]
    public async Task SaveAndLoadPlayer_PreservesInventoryFlagsAndKey()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                ForeignKeys = true,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            var store = new SqlitePlayerStore(new DatabaseConfiguration(connectionString));

            var player = HandlerTestHelpers.CreatePlayer("player");
            var room = HandlerTestHelpers.CreateRoom("room");

            var item = new Object
            {
                Name = "chest",
                Description = "A small chest",
                Flags = ObjectFlags.Openable | ObjectFlags.Lockable,
                KeyId = "skeleton-key",
                IsLocked = true
            };

            item.MoveTo(player);

            var snapshot = PlayerSnapshotFactory.CreateNewPlayer(player, room, "password");

            await store.SaveNewPlayerAsync(snapshot);

            var loaded = await store.LoadPlayerAsync(new LoginRequest(player.Username, "password"));

            Assert.NotNull(loaded);

            var loadedItem = Assert.Single(loaded.Inventory);

            Assert.Equal(item.Id.Value.ToString(), loadedItem.Id);
            Assert.Equal(item.Flags, (ObjectFlags)loadedItem.Flags);
            Assert.Equal(item.KeyId, loadedItem.KeyId);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}

public class SqliteWorldStoreObjectPersistenceTests
{
    [Fact]
    public async Task SaveAndLoadRooms_PreservesObjectState()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                ForeignKeys = true,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            var store = new SqliteWorldStore(new DatabaseConfiguration(connectionString));

            var room = HandlerTestHelpers.CreateRoom("room");

            var item = new Object
            {
                Name = "lantern",
                Description = "An old lantern",
                Flags = ObjectFlags.LightSource | ObjectFlags.Scenery,
                CreatorUsername = "builder",
                IsOpenable = true
            };

            item.MoveTo(room);

            await store.SaveRoomsAsync(WorldSnapshotFactory.CreateSnapshots([room]));

            var loadedSnapshots = await store.LoadRoomsAsync();
            var loadedRooms = WorldSnapshotFactory.CreateRooms(loadedSnapshots);
            var loadedRoom = Assert.Single(loadedRooms);
            var loadedItem = Assert.Single(loadedRoom.Contents);

            Assert.Equal(item.Id, loadedItem.Id);
            Assert.Equal(item.Flags, loadedItem.Flags);
            Assert.Equal(item.KeyId, loadedItem.KeyId);
            Assert.Equal(item.CreatorUsername, loadedItem.CreatorUsername);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
