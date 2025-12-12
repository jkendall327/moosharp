using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Actors.Players;
using MooSharp.Data;
using MooSharp.Data.Players;
using MooSharp.Infrastructure;
using MooSharp.Infrastructure.Sessions;

namespace MooSharp.Agents;

public class AgentSpawner(
    AgentFactory factory,
    ISessionGateway gateway,
    World.World world,
    IPlayerRepository playerRepository,
    IOptions<AgentOptions> options,
    ILogger<AgentSpawner> logger)
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var identities = await LoadIdentitiesAsync(stoppingToken);

        var agentTasks = new List<Task>();

        foreach (var identity in identities)
        {
            var brain = factory.Build(identity);

            var playerId = await EnsureAgentExists(identity, stoppingToken);

            // Hook up the output channel
            var channel = new AgentOutputChannel(msg => brain
                .EnqueueMessageAsync(msg, stoppingToken)
                .AsTask());

            await gateway.OnSessionStartedAsync(playerId, channel);

            // Start the brain's main loop and track the task
            // We do NOT await here, or we'd block the loop.
            var agentLoopTask = brain.RunAsync(playerId, stoppingToken);
            agentTasks.Add(agentLoopTask);

            logger.LogInformation("Agent {Name} started", identity.Name);
        }

        try
        {
            // Wait for cancellation (shutdown)
            await Task.WhenAll(agentTasks);
            logger.LogInformation("All agent tasks done, exiting");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Agent background service cancelled, shutting down");
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
            MooSharpJsonSerializerOptions.Options,
            cancellationToken: cancellationToken) ?? [];

        return identities;
    }

    private async Task<Guid> EnsureAgentExists(AgentIdentity identity, CancellationToken cancellationToken)
    {
        var player = await playerRepository.GetPlayerByUsername(identity.Name, cancellationToken);

        var id = player?.Id ?? Guid.NewGuid();

        if (player is null)
        {
            await PersistAgentToDatabase(id, identity, cancellationToken);
        }

        return id;
    }

    private async Task PersistAgentToDatabase(Guid id, AgentIdentity identity, CancellationToken cancellationToken)
    {
        var defaultRoom = world.GetDefaultRoom().Id.Value;

        var startingRoom = identity.StartingRoomSlug ?? defaultRoom;

        var password = Random.Shared.GetHexString(12);
        var req = new NewPlayerRequest(id, identity.Name, password, startingRoom, identity.Persona);

        await playerRepository.SaveNewPlayerAsync(req, WriteType.Immediate, cancellationToken);
    }
}