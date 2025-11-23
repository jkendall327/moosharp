using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MooSharp.Messaging;
using MooSharp.Persistence;

namespace MooSharp.Agents;

public class AgentService(World world, ChannelWriter<GameInput> writer, IPlayerStore store, IServiceProvider services)
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create an Agent
        await SpawnAgent("Gandalf",
            "You are a wise wizard. You speak in riddles. You enjoy examining things.",
            stoppingToken);

        await SpawnAgent("Gollum",
            "You are obsessed with finding your precious. You are sneaky and rude.",
            stoppingToken);
    }

    private async Task SpawnAgent(string name, string persona, CancellationToken ct)
    {
        // Setup Semantic Kernel (Assuming you registered Kernel in DI)
        using var scope = services.CreateScope();
        var kernel = scope.ServiceProvider.GetRequiredService<Kernel>();
        var chat = scope.ServiceProvider.GetRequiredService<IChatCompletionService>();

        // Create the Brain
        var brain = new AgentBrain(name, persona, writer, chat, kernel);

        var player = new Player
        {
            Username = name,
            Connection = brain.Connection,
            CurrentLocation = world.Rooms.First()
                .Value
        };
        
        world.Players.Add(null, player);
        
        // Register in the World (Same logic as CreateNewPlayer in GameEngine)
        // Note: You might need to refactor CreateNewPlayer logic to be reusable 
        // or send a 'RegisterCommand' through the writer.

        // Sending a Register command allows the GameEngine to handle the specific logic
        await writer.WriteAsync(new GameInput(new ConnectionId(brain.Connection.Id),
                new RegisterCommand
                {
                    Username = name,
                    Password = "agent-password"
                }),
            ct);

        // Hack: We need to inject the connection implementation into the Player object
        // The GameEngine creates the Player object. 
        // You might need to adjust GameEngine.CreateNewPlayer to accept an existing IPlayerConnection
        // or look it up from a shared registry.
    }
}