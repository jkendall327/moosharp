using System.Text.Json;
using System.Text.Json.Serialization;
using MooSharp.Actors.Objects;
using MooSharp.Actors.Rooms;
using MooSharp.Agents;

namespace MooSharp.Infrastructure;

public static class MooSharpJsonSerializerOptions
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        Converters =
        {
            new JsonStringEnumConverter<AgentSource>(),
            new JsonStringEnumConverter<ObjectFlags>(),
            new RoomIdJsonConverter()
        }
    };
}