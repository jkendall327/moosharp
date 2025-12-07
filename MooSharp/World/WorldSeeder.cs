using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Actors.Objects;
using MooSharp.Actors.Rooms;
using MooSharp.Infrastructure;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.World;

public interface IWorldSeeder
{
    IReadOnlyCollection<Room> GetSeedRooms();
}

public class WorldSeeder(IOptions<AppOptions> options, ILogger<WorldSeeder> logger) : IWorldSeeder
{
    public IReadOnlyCollection<Room> GetSeedRooms()
    {
        var dto = GetWorldDto();

        var rooms = CreateRooms(dto);
        CreateObjects(dto, rooms);

        return rooms.Values.ToList();
    }

    private WorldDto GetWorldDto()
    {
        var path = options.Value.WorldDataFilepath;

        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("WorldDataFilepath is not set.");
        }

        var raw = File.ReadAllText(path);

        var dto = JsonSerializer.Deserialize<WorldDto>(raw, MooSharpJsonSerializerOptions.Options);

        if (dto is null)
        {
            throw new InvalidOperationException("World data failed to deserialize.");
        }

        if (!dto.Rooms.Any())
        {
            throw new InvalidOperationException("Room data failed to deserialize.");
        }

        var slugs = dto
            .Rooms
            .Select(s => s.Slug)
            .ToList();

        var filtered = slugs
            .Distinct()
            .ToList();

        if (slugs.Count != filtered.Count)
        {
            throw new InvalidOperationException("At least one slug was duplicated across rooms.");
        }

        return dto;
    }

    private Dictionary<RoomId, Room> CreateRooms(WorldDto dto)
    {
        var roomActorsBySlug = dto.Rooms.ToDictionary(r => r.Slug,
            r => new Room
            {
                Id = r.Slug,
                Name = r.Name,
                Description = r.Description,
                LongDescription = r.LongDescription,
                EnterText = r.EnterText,
                ExitText = r.ExitText,
                CreatorUsername = r.CreatorUsername
            });

        foreach (var roomDto in dto.Rooms)
        {
            try
            {
                logger.LogInformation("Initializing exits for {Room}", roomDto.Slug);

                var currentRoomActor = roomActorsBySlug[roomDto.Slug];

                foreach (var exitDto in roomDto.Exits)
                {
                    if (!roomActorsBySlug.TryGetValue(exitDto.DestinationSlug, out var destinationRoom))
                    {
                        logger.LogError("Missing slug {Slug} for room {Room}", exitDto.DestinationSlug, roomDto.Slug);
                        continue;
                    }

                    var aliases = exitDto.Aliases.Any()
                        ? exitDto.Aliases.ToList()
                        : new();

                    if (!string.IsNullOrWhiteSpace(exitDto.Direction))
                    {
                        var abbreviation = exitDto.Direction[0].ToString();

                        if (!aliases.Contains(abbreviation, StringComparer.OrdinalIgnoreCase))
                        {
                            aliases.Add(abbreviation);
                        }
                    }

                    var exit = new Exit
                    {
                        Name = exitDto.Direction,
                        Description = exitDto.Description,
                        Destination = destinationRoom.Id,
                        Aliases = aliases,
                        Keywords = exitDto.Keywords.ToList(),
                        IsHidden = exitDto.IsHidden,
                        IsLocked = exitDto.IsLocked,
                        IsOpen = exitDto.IsLocked ? false : exitDto.IsOpen,
                        KeyId = exitDto.KeyId,
                        CanBeLocked = exitDto.IsLocked || !string.IsNullOrWhiteSpace(exitDto.KeyId)
                    };

                    currentRoomActor.Exits.Add(exit);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize exits for {Room}", roomDto.Slug);

                throw;
            }
        }

        return roomActorsBySlug;
    }

    private void CreateObjects(WorldDto dto, Dictionary<RoomId, Room> rooms)
    {
        foreach (var objectDto in dto.Objects.Where(s => s.RoomSlug is not null))
        {
            if (!rooms.TryGetValue(objectDto.RoomSlug!.Value, out var room))
            {
                continue;
            }

            var obj = new Object
            {
                Description = objectDto.Description,
                Name = objectDto.Name,
                Flags = objectDto.Flags,
                KeyId = objectDto.KeyId,
                CreatorUsername = objectDto.CreatorUsername
            };

            if (!string.IsNullOrWhiteSpace(objectDto.TextContent))
            {
                obj.WriteText(objectDto.TextContent);
            }

            obj.MoveTo(room);
        }
    }
}
