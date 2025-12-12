using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MooSharp.Actors.Players;
using MooSharp.Commands.Commands.Scripting;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Features.Editor;
using MooSharp.Infrastructure.Messaging;
using MooSharp.Scripting;

namespace MooSharp.Game;

public class GameInputProcessor(
    World.World world,
    CommandParser parser,
    CommandExecutor executor,
    IGameMessageEmitter emitter,
    IVerbScriptResolver verbScriptResolver,
    IEditorModeService editorModeService,
    IEditorModeHandler editorModeHandler,
    ILogger<GameInputProcessor> logger)
{
    public async Task ProcessInputAsync(InputCommand inputCommand, CancellationToken ct = default)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            { "ActorId", inputCommand.ActorId },
            { "Command", inputCommand.Command }
        });

        logger.LogDebug("Processing input command");

        var player = world.TryGetPlayer(inputCommand.ActorId);

        if (player is not null)
        {
            // Check if player is in editor mode first - input should be handled differently
            if (editorModeService.IsInEditorMode(inputCommand.ActorId))
            {
                logger.LogDebug("Actor is in editor mode, routing to editor handler");
                await editorModeHandler.HandleEditorInputAsync(player, inputCommand.Command, ct);
                return;
            }

            await ProcessWorldCommand(player, inputCommand.Command, ct);
        }
        else
        {
            logger.LogWarning("Actor {ActorId} not found in world", inputCommand.ActorId);
            throw new InvalidOperationException(
                $"Got game input for actor {inputCommand.ActorId}, but they were not found in the world.");
        }
    }

    private async Task ProcessWorldCommand(Player player, string command, CancellationToken ct = default)
    {
        // 1. Parse returns a Result object now, not just ICommand?
        var parseResult = await parser.ParseAsync(player, command, ct);

        logger.LogDebug("Parse completed with status {ParseStatus}", parseResult.Status);

        // 2. Handle the specific outcome
        switch (parseResult.Status)
        {
            case ParseStatus.Success:
                try
                {
                    player.LastActionAt = DateTime.UtcNow;

                    // We are guaranteed a Command here because of the Status check
                    var result = await executor.Handle(parseResult.Command!, ct);
                    await ProcessResultAsync(result, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing world command");
                    var unexpected = new GameMessage(player, new SystemMessageEvent("An unexpected error occurred."));
                    _ = emitter.SendGameMessagesAsync([unexpected], ct);
                }

                break;

            case ParseStatus.Error:
                // The user typed a valid verb (e.g., "give") but failed binding ("give ghost")
                // The parser has generated a specific, helpful error message.
                logger.LogDebug("Parse error: {ErrorMessage}", parseResult.ErrorMessage);
                var errorMsg = new GameMessage(player, new SystemMessageEvent(parseResult.ErrorMessage!));
                _ = emitter.SendGameMessagesAsync([errorMsg], ct);

                break;

            case ParseStatus.NotFound:
                // Try to find a script-based verb before giving up
                var room = world.GetLocationOrThrow(player);
                var scriptCommand = verbScriptResolver.TryResolveCommand(player, room, command);
                if (scriptCommand is not null)
                {
                    logger.LogDebug("Verb not found in built-in commands, resolved to script command");
                    try
                    {
                        player.LastActionAt = DateTime.UtcNow;

                        var scriptResult = await executor.Handle(scriptCommand, ct);
                        await ProcessResultAsync(scriptResult, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error executing script command");
                        var scriptError = new GameMessage(player, new SystemMessageEvent("An error occurred while running the script."));
                        _ = emitter.SendGameMessagesAsync([scriptError], ct);
                    }
                }
                else
                {
                    logger.LogDebug("Command not recognised");
                    // The user typed gibberish or a command that doesn't exist
                    var notFoundMsg = new GameMessage(player, new SystemMessageEvent("I don't understand that command."));
                    _ = emitter.SendGameMessagesAsync([notFoundMsg], ct);
                }

                break;
        }
    }

    private async Task ProcessResultAsync(CommandResult result, CancellationToken ct)
    {
        var commandQueue = new Queue<ICommand>(result.CommandsToQueue);

        if (commandQueue.Count > 0)
        {
            logger.LogDebug("Command queued {QueuedCommandCount} follow-up commands", commandQueue.Count);
        }

        await emitter.SendGameMessagesAsync(result.Messages, ct);

        while (commandQueue.Count > 0)
        {
            var queuedCommand = commandQueue.Dequeue();

            logger.LogDebug("Processing queued command {QueuedCommandType}", queuedCommand.GetType().Name);

            try
            {
                var queuedResult = await executor.Handle(queuedCommand, ct);

                await emitter.SendGameMessagesAsync(queuedResult.Messages, ct);

                foreach (var followUp in queuedResult.CommandsToQueue)
                {
                    commandQueue.Enqueue(followUp);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing queued command {QueuedCommandType}", queuedCommand.GetType().Name);
            }
        }
    }
}