using System.Text;

namespace MooSharp;

public class ExamineCommand : ICommand
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class ExamineHandler(StringProvider provider) : IHandler<ExamineCommand>
{
    public Task Handle(ExamineCommand cmd, StringBuilder buffer, CancellationToken cancellationToken = default)
    {
        var player = cmd.Player;

        if (cmd.Target is "me")
        {
            buffer.AppendLine(provider.ExamineSelf());

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