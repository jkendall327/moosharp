using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using MooSharp.Actors;
using MooSharp.Data;
using MooSharp.Data.Dtos;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using MooSharp.Web.Services;

namespace MooSharp.Agents;

public class AgentSpawner(
    AgentFactory factory,
    ISessionGateway gateway,
    World.World world,
    IPlayerRepository playerRepository,
    IOptions<AgentOptions> options)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter<AgentSource>()
        }
    };

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var identities = await LoadIdentitiesAsync(stoppingToken);

        foreach (var identity in identities)
        {
            stoppingToken.ThrowIfCancellationRequested();

            await SpawnAgentAsync(identity, stoppingToken);
        }
    }

    private async Task<IReadOnlyCollection<AgentIdentity>> LoadIdentitiesAsync(CancellationToken cancellationToken)
    {
        var path = options.Value.AgentIdentitiesPath;

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Agent identity file not found at {path}", path);
        }

        await using var stream = File.OpenRead(path);

        var identities = await JsonSerializer.DeserializeAsync<List<AgentIdentity>>(stream,
            JsonSerializerOptions,
            cancellationToken: cancellationToken) ?? [];

        return identities;
    }

    private async Task SpawnAgentAsync(AgentIdentity identity, CancellationToken cancellationToken)
    {
        var brain = factory.Build(identity);

        await brain.StartAsync(cancellationToken);

        var alreadyExists = await playerRepository.PlayerWithUsernameExistsAsync(identity.Name, cancellationToken);

        if (!alreadyExists)
        {
            await PersistAgentToDatabase(brain.Id.Value, identity, cancellationToken);
        }

        var channel = new AgentOutputChannel(brain.WriteToInternalQueue);
        await gateway.OnSessionStartedAsync(brain.Id.Value, channel);
    }

    private async Task PersistAgentToDatabase(Guid id, AgentIdentity identity, CancellationToken cancellationToken)
    {
        // Stick the agent in the database so they are spawned into the world properly.
        // TODO: unsure if this feels like a hack or not?
        var startingRoom = identity.StartingRoomSlug ?? world.GetDefaultRoom()
            .Id.Value;

        var req = new NewPlayerRequest(id, identity.Name, Random.Shared.GetHexString(12), startingRoom);

        await playerRepository.SaveNewPlayerAsync(req, WriteType.Immediate, cancellationToken);
    }
}