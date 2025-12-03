using Microsoft.EntityFrameworkCore;
using MooSharp.Data.Dtos;

namespace MooSharp.Data.EntityFramework;

internal sealed class EfPlayerStore(IDbContextFactory<MooSharpDbContext> contextFactory) : IPlayerStore
{
    public async Task SaveNewPlayerAsync(NewPlayerRequest player, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var existing = await context.Players
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Username == player.Username, ct);

        if (existing is null)
        {
            existing = new PlayerEntity
            {
                Username = player.Username
            };
            context.Players.Add(existing);
        }

        existing.Password = BCrypt.Net.BCrypt.HashPassword(player.Password);
        existing.CurrentLocation = player.CurrentLocation;

        context.PlayerInventory.RemoveRange(existing.Inventory);
        existing.Inventory = player.Inventory
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

    public async Task SavePlayerAsync(PlayerSnapshotDto snapshot, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var player = await context.Players
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Username == snapshot.Username, ct);

        if (player is null)
        {
            return;
        }

        player.CurrentLocation = snapshot.CurrentLocation;

        context.PlayerInventory.RemoveRange(player.Inventory);
        player.Inventory = snapshot.Inventory
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

    public async Task<PlayerDto?> LoadPlayerAsync(LoginRequest command, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var player = await context.Players
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Username == command.Username, ct);

        if (player is null || !BCrypt.Net.BCrypt.Verify(command.Password, player.Password))
        {
            return null;
        }

        var inventory = player.Inventory
            .Select(i => new InventoryItemDto(
                i.ItemId,
                i.Name,
                i.Description,
                i.TextContent,
                i.Flags,
                i.KeyId,
                i.CreatorUsername))
            .ToList();

        return new PlayerDto(player.Username, player.Password, player.CurrentLocation, inventory);
    }
}
