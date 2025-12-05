using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MooSharp.Data.EntityFramework;

internal sealed class MooSharpDbContext(DbContextOptions<MooSharpDbContext> options) : DbContext(options)
{
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<InventoryItemEntity> PlayerInventory => Set<InventoryItemEntity>();
    public DbSet<RoomEntity> Rooms => Set<RoomEntity>();
    public DbSet<ExitEntity> Exits => Set<ExitEntity>();
    public DbSet<ObjectEntity> Objects => Set<ObjectEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(e => e.Username);
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.CurrentLocation).IsRequired();
            entity.HasMany(e => e.Inventory)
                .WithOne(i => i.Player)
                .HasForeignKey(i => i.Username)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InventoryItemEntity>(entity =>
        {
            entity.HasKey(e => e.ItemId);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Flags).IsRequired();
        });

        modelBuilder.Entity<RoomEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.LongDescription).IsRequired();
            entity.Property(e => e.EnterText).IsRequired();
            entity.Property(e => e.ExitText).IsRequired();
            entity.HasMany(e => e.Objects)
                .WithOne(o => o.Room)
                .HasForeignKey(o => o.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExitEntity>(entity =>
        {
            entity.HasKey(e => new { e.FromRoomId, e.ToRoomId });
            entity.HasOne<RoomEntity>()
                .WithMany(r => r.Exits)
                .HasForeignKey(e => e.FromRoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<RoomEntity>()
                .WithMany()
                .HasForeignKey(e => e.ToRoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ObjectEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Flags).IsRequired();
        });
    }
}

public sealed class PlayerEntity
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CurrentLocation { get; set; } = string.Empty;
    public List<InventoryItemEntity> Inventory { get; set; } = [];
}

public sealed class InventoryItemEntity
{
    public string ItemId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public int Flags { get; set; }
    public string? KeyId { get; set; }
    public string? CreatorUsername { get; set; }
    public PlayerEntity? Player { get; set; }
}

public sealed class RoomEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string EnterText { get; set; } = string.Empty;
    public string ExitText { get; set; } = string.Empty;
    public string? CreatorUsername { get; set; }
    public List<ExitEntity> Exits { get; set; } = [];
    public List<ObjectEntity> Objects { get; set; } = [];
}

public sealed class ExitEntity
{
    public string FromRoomId { get; set; } = string.Empty;
    public string ToRoomId { get; set; } = string.Empty;
}

public sealed class ObjectEntity
{
    public string Id { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public int Flags { get; set; }
    public string? KeyId { get; set; }
    public string? CreatorUsername { get; set; }
    public RoomEntity? Room { get; set; }
}
