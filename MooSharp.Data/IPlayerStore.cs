using Microsoft.EntityFrameworkCore;
using MooSharp.Data.Dtos;
using MooSharp.Data.EntityFramework;

namespace MooSharp.Data;

internal interface IPlayerStore
{
    Task SaveNewPlayerAsync(NewPlayerRequest player, CancellationToken ct);
    Task SavePlayerAsync(PlayerSnapshotDto snapshot, CancellationToken ct);
    Task<PlayerDto?> LoadPlayerAsync(string username, string password, CancellationToken ct);
}

internal class EfPlayerStore(IDbContextFactory<MooSharpDbContext> contextFactory) : IPlayerStore
{
    private static readonly string FakeBCryptHash = "$2a$11$dkL4OYJdQeDVNvTqK8Pz0Oz1b1ewy6/8.GkFzZb1sPmGlLP3lE8gm";

    public async Task SaveNewPlayerAsync(NewPlayerRequest player, CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var existing = await context
            .Players
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Username == player.Username, ct);

        if (existing is null)
        {
            existing = new()
            {
                Username = player.Username
            };

            context.Players.Add(existing);
        }

        existing.Password = BCrypt.Net.BCrypt.HashPassword(player.Password);
        existing.CurrentLocation = player.CurrentLocation;

        context.PlayerInventory.RemoveRange(existing.Inventory);

        existing.Inventory = player
            .Inventory
            .Select(i => new InventoryItemEntity
            {
                ItemId = i.Id,
                Username = player.Username,
                Name = i.Name,
                Description = i.Description,
                TextContent = i.TextContent,
                Flags = i.Flags,
                KeyId = i.KeyId,
                CreatorUsername = i.CreatorUsername
            })
            .ToList();

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
            return;
        }

        player.CurrentLocation = snapshot.CurrentLocation;

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
                CreatorUsername = i.CreatorUsername
            })
            .ToList();

        await context.SaveChangesAsync(ct);
    }

    public async Task<PlayerDto?> LoadPlayerAsync(string username, string password, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var player = await context
            .Players
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Username == username, ct);

        // Always perform hash verification to mitigate timing side-channel.
        // This method isn't constant-time or anything but might as well go partway.
        var target = player?.Password ?? FakeBCryptHash;
        var ok = BCrypt.Net.BCrypt.Verify(target, password);

        if (player is null || !ok)
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
                i.CreatorUsername))
            .ToList();

        return new(player.Username, player.Password, player.CurrentLocation, inventory);
    }
}