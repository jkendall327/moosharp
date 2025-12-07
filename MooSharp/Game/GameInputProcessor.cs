using Microsoft.Extensions.Logging;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Infrastructure.Messaging;

namespace MooSharp.Game;

public class GameInputProcessor(
    World.World world,
    CommandParser parser,
    CommandExecutor executor,
    IGameMessageEmitter emitter,
    ILogger<GameInputProcessor> logger)
{
    public async Task ProcessInputAsync(InputCommand inputCommand, CancellationToken ct = default)
    {
        var player = world.TryGetPlayer(inputCommand.ActorId);

        if (player is not null)
        {
            await ProcessWorldCommand(player, inputCommand.Command, ct);
        }
        else
        {
            throw new InvalidOperationException(
                $"Got game input for actor {inputCommand.ActorId}, but they were not found in the world.");
        }
    }

    private async Task ProcessWorldCommand(Player player, string command, CancellationToken ct = default)
    {
        // 1. Parse returns a Result object now, not just ICommand?
        var parseResult = await parser.ParseAsync(player, command, ct);

        // 2. Handle the specific outcome
        switch (parseResult.Status)
        {
            case ParseStatus.Success:
                try
                {
                    // We are guaranteed a Command here because of the Status check
                    var result = await executor.Handle(parseResult.Command!, ct);
                    _ = emitter.SendGameMessagesAsync(result.Messages, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing world command {Command}", command);
                    var unexpected = new GameMessage(player, new SystemMessageEvent("An unexpected error occurred."));
                    _ = emitter.SendGameMessagesAsync([unexpected], ct);
                }

                break;

            case ParseStatus.Error:
                // The user typed a valid verb (e.g., "give") but failed binding ("give ghost")
                // The parser has generated a specific, helpful error message.
                var errorMsg = new GameMessage(player, new SystemMessageEvent(parseResult.ErrorMessage!));
                _ = emitter.SendGameMessagesAsync([errorMsg], ct);

                break;

            case ParseStatus.NotFound:
                // The user typed gibberish or a command that doesn't exist
                var notFoundMsg = new GameMessage(player, new SystemMessageEvent("I don't understand that command."));
                _ = emitter.SendGameMessagesAsync([notFoundMsg], ct);

                break;
        }
    }
}