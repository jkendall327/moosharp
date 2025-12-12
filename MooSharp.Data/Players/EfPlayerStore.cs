using Microsoft.EntityFrameworkCore;
using MooSharp.Data.EntityFramework;

namespace MooSharp.Data.Players;

internal class EfPlayerStore(IDbContextFactory<MooSharpDbContext> contextFactory) : IPlayerStore, ILoginChecker
{
    private static readonly string FakeBCryptHash = "$2a$11$dkL4OYJdQeDVNvTqK8Pz0Oz1b1ewy6/8.GkFzZb1sPmGlLP3lE8gm";

    public async Task SaveNewPlayerAsync(NewPlayerRequest player, CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        // Hash unconditionally to mitigate timing side-channels.
        var hashed = BCrypt.Net.BCrypt.HashPassword(player.Password);

        var existing = await context.Players.AnyAsync(p => p.Username == player.Username, ct);

        if (existing)
        {
            throw new InvalidOperationException($"Player record with username {player.Username} already exists.");
        }

        var newPlayer = new PlayerEntity
        {
            Id = player.Id,
            Username = player.Username,
            Password = hashed,
            Description = player.Description,
            CurrentLocation = player.CurrentLocation,
            Inventory = []
        };

        context.Players.Add(newPlayer);
        await context.SaveChangesAsync(ct);
    }

    public async Task SavePlayerAsync(PlayerSnapshotDto snapshot, CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var player = await context
            .Players
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Username == snapshot.Username, ct);

        if (player is null)
        {
            throw new InvalidOperationException($"No player record was found for username {snapshot.Username}");
        }

        player.CurrentLocation = snapshot.CurrentLocation;
        player.Description = snapshot.Description;

        context.PlayerInventory.RemoveRange(player.Inventory);

        player.Inventory = snapshot
            .Inventory
            .Select(i => new InventoryItemEntity
            {
                ItemId = i.Id,
                Username = snapshot.Username,
                Name = i.Name,
                Description = i.Description,
                TextContent = i.TextContent,
                Flags = i.Flags,
                KeyId = i.KeyId,
                CreatorUsername = i.CreatorUsername,
                DynamicPropertiesJson = i.DynamicPropertiesJson,
                VerbScriptsJson = i.VerbScriptsJson
            })
            .ToList();

        await context.SaveChangesAsync(ct);
    }

    public async Task<PlayerDto?> LoadPlayerAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var player = await context
            .Players
            .Include(p => p.Inventory)
            .SingleOrDefaultAsync(p => p.Id == id, ct);

        if (player is null)
        {
            return null;
        }

        var inventory = player
            .Inventory
            .Select(i => new InventoryItemDto(
                i.ItemId,
                i.Name,
                i.Description,
                i.TextContent,
                i.Flags,
                i.KeyId,
                i.CreatorUsername,
                i.DynamicPropertiesJson,
                i.VerbScriptsJson))
            .ToList();

        return new(player.Id, player.Username, player.Description, player.CurrentLocation, inventory);
    }

    public async Task<PlayerDto?> GetPlayerByUsernameAsync(string username, CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var player = await context.Players.SingleOrDefaultAsync(p => p.Username == username, ct);

        if (player is null)
        {
            return null;
        }

        return new(player.Id, player.Username, player.Description, player.CurrentLocation, []);
    }

    public async Task UpdatePlayerDescriptionAsync(Guid playerId, string description, CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var player = await context
            .Players
            .SingleOrDefaultAsync(p => p.Id == playerId, ct);

        if (player is null)
        {
            throw new InvalidOperationException($"No player record was found for player ID {playerId}");
        }

        player.Description = description;

        await context.SaveChangesAsync(ct);
    }

    public async Task<LoginResult> LoginIsValidAsync(string username, string password, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var player = await context
            .Players
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Username == username, ct);

        // Always perform hash verification to mitigate timing side-channel.
        // This method isn't constant-time or anything but might as well go partway.
        var target = player?.Password ?? FakeBCryptHash;
        var ok = BCrypt.Net.BCrypt.Verify(password, target);

        if (player is null)
        {
            return LoginResult.UsernameNotFound;
        }

        if (!ok)
        {
            return LoginResult.WrongPassword;
        }

        return LoginResult.Ok;
    }
}