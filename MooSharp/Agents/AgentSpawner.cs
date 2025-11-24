namespace MooSharp.Agents;

using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using MooSharp;
using MooSharp.Messaging;

public class AgentSpawner(AgentFactory factory, ChannelWriter<GameInput> writer, IOptions<AgentOptions> options)
{
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

        var identities = await JsonSerializer.DeserializeAsync<List<AgentIdentity>>(stream, cancellationToken: cancellationToken)
            ?? [];

        return identities;
    }

    private async Task SpawnAgentAsync(AgentIdentity identity, CancellationToken cancellationToken)
    {
        var brain = factory.Build(identity);

        await brain.StartAsync(cancellationToken);
        
        var registerAgentCommand = new RegisterAgentCommand
        {
            Identity = identity,
            Connection = brain.Connection
        };

        var connectionId = new ConnectionId(brain.Connection.Id);

        await writer.WriteAsync(new(connectionId, registerAgentCommand), cancellationToken);
    }
}