using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using MooSharp.Actors;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using MooSharp.Web.Services;

namespace MooSharp.Agents;

public class AgentSpawner(
    AgentFactory factory,
    ISessionGateway gateway,
    ChannelWriter<GameInput> writer,
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

        var registerAgentCommand = new RegisterAgentCommand
        {
            Identity = identity,
            Connection = brain.Connection
        };

        var channel = new AgentOutputChannel(brain.WriteToInternalQueue);
        await gateway.OnSessionStartedAsync(Guid.NewGuid(), channel);

        var connectionId = new ConnectionId(brain.Connection.Id);

        await writer.WriteAsync(new(connectionId, registerAgentCommand), cancellationToken);

        throw new NotImplementedException("Remove the old connection stuff here.");
    }
}