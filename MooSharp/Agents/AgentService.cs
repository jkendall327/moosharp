using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MooSharp.Agents;

public class AgentService(World world, ChannelWriter<GameInput> writer, IServiceProvider services)
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
        using var scope = services.CreateScope();
        var kernel = scope.ServiceProvider.GetRequiredService<Kernel>();
        var chat = scope.ServiceProvider.GetRequiredService<IChatCompletionService>();

        var brain = new AgentBrain(name, persona, writer, chat, kernel);

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