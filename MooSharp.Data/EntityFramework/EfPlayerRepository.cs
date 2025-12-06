using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooSharp.Data.Dtos;

namespace MooSharp.Data.EntityFramework;

internal sealed class EfPlayerRepository(
    ChannelWriter<DatabaseRequest> writer,
    IDbContextFactory<MooSharpDbContext> contextFactory,
    ILogger<EfPlayerRepository> logger) : IPlayerRepository
{
    private static readonly string FakeBCryptHash = "$2a$11$dkL4OYJdQeDVNvTqK8Pz0Oz1b1ewy6/8.GkFzZb1sPmGlLP3lE8gm";

    public async Task SaveNewPlayerAsync(NewPlayerRequest player,
        WriteType type = WriteType.Deferred,
        CancellationToken ct = default)
    {
        if (type is WriteType.Deferred)
        {
            await EnqueueAsync(new SaveNewPlayerRequest(player), ct);
            return;
        }
        
        await SaveNewPlayerInner(player, ct);
    }

    public async Task SavePlayerAsync(PlayerSnapshotDto snapshot,
        WriteType type = WriteType.Deferred,
        CancellationToken ct = default)
    {
        if (type is WriteType.Deferred)
        {
            await EnqueueAsync(new SavePlayerRequest(snapshot), ct);
            return;
        }

        await SavePlayerInner(snapshot, ct);
    }
    
    private async Task EnqueueAsync(DatabaseRequest request, CancellationToken ct)
    {
        try
        {
            await writer.WriteAsync(request, ct);
        }
        catch (ChannelClosedException)
        {
            logger.LogWarning("Database request channel was closed; request {RequestType} was dropped", request.GetType().Name);
        }
    }

    private async Task SaveNewPlayerInner(NewPlayerRequest player, CancellationToken ct)
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

    private async Task SavePlayerInner(PlayerSnapshotDto snapshot, CancellationToken ct)
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

    public async Task<PlayerDto?> LoadPlayerAsync(LoginRequest command, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var player = await context
            .Players
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Username == command.Username, ct);

        // Always perform hash verification to mitigate timing side-channel.
        // This method isn't constant-time or anything but might as well go partway.
        var password = player?.Password ?? FakeBCryptHash;
        var ok = BCrypt.Net.BCrypt.Verify(command.Password, password);

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