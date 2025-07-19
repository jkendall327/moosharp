using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MooSharp;

public class CommandExecutor(IServiceProvider serviceProvider, ILogger<CommandExecutor> logger)
{
    public async Task Handle<TCommand>(TCommand command, StringBuilder buffer, CancellationToken token = default)
        where TCommand : ICommand
    {
        var handler = serviceProvider.GetService<IHandler<TCommand>>();

        if (handler is null)
        {
            logger.LogError("Handler not found: {HandlerType}",  command.GetType().Name);
            throw new InvalidOperationException("Handler not found");
        }

        await handler.Handle(command, buffer, token);
    }
}

public class CommandParser(World world, PlayerMultiplexer multiplexer, CommandExecutor executor, ILogger<CommandParser> logger)
{
    public async Task ParseAsync(Player player, string command, CancellationToken token = default)
    {
        var sb = new StringBuilder();

        player.CurrentLocation ??= world.Rooms.First();
        
        logger.LogDebug("Parsing player input: {Input}", command);
        
        var split = command.Split(' ');

        var verb = split.FirstOrDefault();

        // TODO: handle split.count > 2.
        
        ICommand? cmd = verb switch
        {
            "move" => new MoveCommand
            {
                Player = player,
                Origin = player.CurrentLocation,
                TargetExit = split.Last()
            },
            "examine" => new ExamineCommand
            {
                Player = player,
                Target = split.Last()
            },
            "take" => new TakeCommand
            {
                Player = player,
                Target = split.Last()
            },
            _ => null
        };

        if (cmd is not null)
        {
            logger.LogDebug("Parsed input to command {CommandType}",  cmd.GetType().Name);
            
            // This switch expression is just here so the compiler determines the type of the cmd,
            // and by extension, the generic argument to executor.Handle<T>().
            // This lets the executor get the correct handler implementation from DI.
            var task = cmd switch
            {
                ExamineCommand e => executor.Handle(e, sb, token),
                MoveCommand m => executor.Handle(m, sb, token),
                TakeCommand t => executor.Handle(t, sb, token),
                _ => throw new ArgumentOutOfRangeException(nameof(cmd), "Unrecognised command type")
            };

            await task;
        }
        else
        {
            logger.LogDebug("Failed to parse player input as command");
            sb.AppendLine("That command wasn't recognized. Use 'move' to go between locations.");
        }

        await BuildCurrentRoomDescription(player, sb);

        await multiplexer.SendMessage(player, sb, token);
    }

    private static async Task BuildCurrentRoomDescription(Player player, StringBuilder sb)
    {
        if (player.CurrentLocation == null)
        {
            throw new InvalidOperationException("Current location not set");
        }

        var room = await player.CurrentLocation.Ask(new RequestMessage<Room, Room>(Task.FromResult));

        sb.AppendLine(room.Description);

        var availableExits = player.GetCurrentlyAvailableExits().Select(s => s.Key).ToArray();

        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits)}";

        sb.AppendLine(availableExitsMessage);
    }
}