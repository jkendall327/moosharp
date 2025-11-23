namespace MooSharp.Agents;

using System.Text.Json;
using Microsoft.Extensions.Options;

public class AgentSpawner(World world, AgentFactory factory, IOptions<AgentOptions> options)
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var identities = await LoadIdentitiesAsync(stoppingToken);

        foreach (var identity in identities)
        {
            stoppingToken.ThrowIfCancellationRequested();

            await SpawnAgent(identity);
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

    private Task SpawnAgent(AgentIdentity identity)
    {
        var brain = factory.Build(identity);

        var currentLocation = world.Rooms.First()
            .Value;

        var player = new Player
        {
            Username = identity.Name,
            Connection = brain.Connection,
            CurrentLocation = currentLocation
        };
        
        currentLocation.PlayersInRoom.Add(player);
        
        world.Players.Add(player.Connection.Id, player);

        return Task.CompletedTask;
    }
}