using Microsoft.Extensions.Options;
using MooSharp.Infrastructure;
using MooSharp.Persistence;

namespace MooSharp.Tests;

public class SqlitePlayerStoreObjectPersistenceTests
{
    [Fact]
    public async Task SaveAndLoadPlayer_PreservesInventoryFlagsAndKey()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");

        try
        {
            var options = Options.Create(new AppOptions
            {
                DatabaseFilepath = databasePath,
                WorldDataFilepath = "world.json"
            });

            var store = new SqlitePlayerStore(options);

            var player = HandlerTestHelpers.CreatePlayer("player");
            var room = HandlerTestHelpers.CreateRoom("room");

            var item = new Object
            {
                Name = "chest",
                Description = "A small chest",
                Flags = ObjectFlags.Openable | ObjectFlags.Lockable,
                KeyId = "skeleton-key"
            };

            item.IsLocked = true;
            item.MoveTo(player);

            await store.SaveNewPlayer(player, room, "password");

            var loaded = await store.LoadPlayer(new()
            {
                Username = player.Username,
                Password = "password"
            });

            Assert.NotNull(loaded);

            var loadedItem = Assert.Single(loaded.Inventory);

            Assert.Equal(item.Id.Value.ToString(), loadedItem.Id);
            Assert.Equal(item.Flags, loadedItem.Flags);
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
            var options = Options.Create(new AppOptions
            {
                DatabaseFilepath = databasePath,
                WorldDataFilepath = "world.json"
            });

            var store = new SqliteWorldStore(options);

            var room = HandlerTestHelpers.CreateRoom("room");

            var item = new Object
            {
                Name = "lantern",
                Description = "An old lantern",
                Flags = ObjectFlags.LightSource | ObjectFlags.Scenery,
                CreatorUsername = "builder"
            };

            item.IsOpenable = true;
            item.MoveTo(room);

            await store.SaveRoomsAsync([room]);

            var loadedRooms = await store.LoadRoomsAsync();
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
