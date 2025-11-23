namespace MooSharp.Agents;

public class AgentSpawner(World world, AgentFactory factory)
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SpawnAgent("Gandalf",
            "You are a wise wizard. You speak in riddles. You enjoy examining things.");

        await SpawnAgent("Gollum",
            "You are obsessed with finding your precious. You are sneaky and rude.");
    }

    private Task SpawnAgent(string name, string persona)
    {
        var brain = factory.Build(name, persona, AgentSource.OpenAI);

        var currentLocation = world.Rooms.First()
            .Value;

        var player = new Player
        {
            Username = name,
            Connection = brain.Connection,
            CurrentLocation = currentLocation
        };
        
        currentLocation.PlayersInRoom.Add(player);
        
        world.Players.Add(player.Connection.Id, player);

        return Task.CompletedTask;
    }
}