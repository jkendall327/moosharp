using System.Text;

namespace MooSharp;

public class ExamineCommand : ICommand
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class ExamineHandler(StringProvider provider, World world) : IHandler<ExamineCommand>
{
    public Task Handle(ExamineCommand cmd, StringBuilder buffer, CancellationToken cancellationToken = default)
    {
        var player = cmd.Player;

        if (cmd.Target is "me")
        {
            buffer.AppendLine(provider.ExamineSelf());

            var items = world.Rooms
                             .SelectMany(s => s.QueryState(q => q.Contents))
                             .Select(s => s.Value)
                             .ToList();

            var mine = items.Where(s => s.QueryState(e => e.Owner == player))
                            .Select(s => s.QueryState(e => e.Description))
                            .ToList();

            if (mine.Any())
            {
                buffer.AppendLine("You have:");

                foreach (var se in mine)
                {
                    buffer.AppendLine(se);
                }
            }

            return Task.CompletedTask;
        }

        if (player.CurrentLocation is null)
        {
            throw new InvalidOperationException("Player location was null during examine command.");
        }

        var contents = player.CurrentLocation.QueryState(s => s.Contents);

        if (contents.TryGetValue(cmd.Target, out var content))
        {
            var description = content.QueryState(s => s.Description);

            buffer.AppendLine(description);
        }

        return Task.CompletedTask;
    }
}