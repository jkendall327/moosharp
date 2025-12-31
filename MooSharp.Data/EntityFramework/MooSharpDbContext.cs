using System.ComponentModel.DataAnnotations;
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

            entity
                .Property(e => e.Password)
                .IsRequired();

            entity
                .Property(e => e.CurrentLocation)
                .IsRequired();

            entity
                .HasMany(e => e.Inventory)
                .WithOne(i => i.Player)
                .HasForeignKey(i => i.Username)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InventoryItemEntity>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity
                .Property(e => e.Name)
                .IsRequired();

            entity
                .Property(e => e.Description)
                .IsRequired();

            entity
                .Property(e => e.Flags)
                .IsRequired();
        });

        modelBuilder.Entity<RoomEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity
                .Property(e => e.Name)
                .IsRequired();

            entity
                .Property(e => e.Description)
                .IsRequired();

            entity
                .Property(e => e.LongDescription)
                .IsRequired();

            entity
                .Property(e => e.EnterText)
                .IsRequired();

            entity
                .Property(e => e.ExitText)
                .IsRequired();

            entity
                .HasMany(e => e.Objects)
                .WithOne(o => o.Room)
                .HasForeignKey(o => o.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExitEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity
                .Property(e => e.Name)
                .IsRequired();

            entity
                .Property(e => e.Description)
                .IsRequired();

            entity
                .Property(e => e.DestinationRoomId)
                .IsRequired();

            entity
                .HasOne<RoomEntity>()
                .WithMany(r => r.Exits)
                .HasForeignKey(e => e.FromRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne<RoomEntity>()
                .WithMany()
                .HasForeignKey(e => e.DestinationRoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ObjectEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity
                .Property(e => e.Name)
                .IsRequired();

            entity
                .Property(e => e.Description)
                .IsRequired();

            entity
                .Property(e => e.Flags)
                .IsRequired();
        });
    }
}

public sealed class PlayerEntity
{
    public required Guid Id { get; init; }
    [MaxLength(100)] public string Username { get; init; } = string.Empty;
    [MaxLength(100)] public string Password { get; set; } = string.Empty;
    [MaxLength(100)] public string CurrentLocation { get; set; } = string.Empty;
    public string MemoriesJson { get; set; } = "[]";
    public List<InventoryItemEntity> Inventory { get; set; } = [];
}

public sealed class InventoryItemEntity
{
    [MaxLength(100)] public string ItemId { get; init; } = string.Empty;
    [MaxLength(100)] public string Username { get; init; } = string.Empty;
    [MaxLength(100)] public string Name { get; init; } = string.Empty;
    [MaxLength(200)] public string Description { get; init; } = string.Empty;
    [MaxLength(200)] public string? TextContent { get; init; }
    public int Flags { get; init; }
    [MaxLength(50)] public string? KeyId { get; init; }
    [MaxLength(100)] public string? CreatorUsername { get; init; }
    public string? DynamicPropertiesJson { get; init; }
    public string? VerbScriptsJson { get; init; }
    public PlayerEntity? Player { get; init; }
}

public sealed class RoomEntity
{
    [MaxLength(100)] public string Id { get; init; } = string.Empty;
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string Description { get; set; } = string.Empty;
    [MaxLength(100)] public string LongDescription { get; set; } = string.Empty;
    [MaxLength(100)] public string EnterText { get; set; } = string.Empty;
    [MaxLength(100)] public string ExitText { get; set; } = string.Empty;
    [MaxLength(100)] public string? CreatorUsername { get; set; }
    public List<ExitEntity> Exits { get; set; } = [];
    public List<ObjectEntity> Objects { get; set; } = [];
}

public sealed class ExitEntity
{
    public Guid Id { get; init; }
    [MaxLength(100)] public string FromRoomId { get; init; } = string.Empty;
    [MaxLength(100)] public string DestinationRoomId { get; init; } = string.Empty;
    [MaxLength(100)] public string Name { get; init; } = string.Empty;
    [MaxLength(200)] public string Description { get; init; } = string.Empty;
    public bool IsHidden { get; init; }
    public bool IsLocked { get; init; }
    public bool IsOpen { get; init; }
    public bool CanBeOpened { get; init; }
    public bool CanBeLocked { get; init; }
    [MaxLength(50)] public string? KeyId { get; init; }
    public string Aliases { get; init; } = string.Empty;
    public string Keywords { get; init; } = string.Empty;
}

public sealed class ObjectEntity
{
    [MaxLength(100)] public string Id { get; init; } = string.Empty;
    [MaxLength(100)] public string RoomId { get; init; } = string.Empty;
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string Description { get; init; } = string.Empty;
    [MaxLength(100)] public string? TextContent { get; init; }
    public int Flags { get; init; }
    [MaxLength(100)] public string? KeyId { get; init; }
    [MaxLength(100)] public string? CreatorUsername { get; init; }
    public string? DynamicPropertiesJson { get; init; }
    public string? VerbScriptsJson { get; init; }
    public RoomEntity? Room { get; init; }
}