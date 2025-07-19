using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace MooSharp;

public class CommandExecutor(IServiceProvider serviceProvider)
{
    public async Task Handle<TCommand>(TCommand command, StringBuilder buffer, CancellationToken token = default)
        where TCommand : ICommand
    {
        var handler = serviceProvider.GetService<IHandler<TCommand>>();

        if (handler is null)
        {
            throw new InvalidOperationException("Handler not found");
        }

        await handler.Handle(command, buffer, token);
    }
}

public class CommandParser(World world, PlayerMultiplexer multiplexer, CommandExecutor executor)
{
    public async Task ParseAsync(Player player, string command, CancellationToken token = default)
    {
        var sb = new StringBuilder();

        player.CurrentLocation ??= world.Rooms.First();

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
            await executor.Handle(cmd, sb, token);
        }
        else
        {
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