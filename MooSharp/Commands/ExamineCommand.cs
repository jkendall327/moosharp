using System.Text;

namespace MooSharp;

public class ExamineCommand : ICommand
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class ExamineHandler(StringProvider provider, World world) : IHandler<ExamineCommand>
{
    public async Task Handle(ExamineCommand cmd, StringBuilder buffer, CancellationToken cancellationToken = default)
    {
        var player = cmd.Player;

        if (cmd.Target is "me")
        {
            buffer.AppendLine(provider.ExamineSelf());

            var mine = player.Inventory
                             .Select(s => s.Value)
                             .ToList();

            var descriptions = mine.Select(s => s.QueryAsync(e => e.Description))
                                   .ToList();

            await Task.WhenAll(descriptions);

            if (descriptions.Any())
            {
                buffer.AppendLine("You have:");

                foreach (var se in descriptions)
                {
                    buffer.AppendLine(await se);
                }
            }
        }

        if (player.CurrentLocation is null)
        {
            throw new InvalidOperationException("Player location was null during examine command.");
        }

        var contents = await player.CurrentLocation.QueryAsync(s => s.Contents);

        if (contents.TryGetValue(cmd.Target, out var content))
        {
            var description = await content.QueryAsync(s => s.Description);

            buffer.AppendLine(description);
        }
    }
}