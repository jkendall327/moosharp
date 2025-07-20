using System.Text;

namespace MooSharp;

public class ExamineCommand : ICommand
{
    public required PlayerActor Player { get; init; }
    public required string Target { get; init; }
}

public class ExamineHandler(StringProvider provider, World world) : IHandler<ExamineCommand>
{
    public async Task Handle(ExamineCommand cmd, StringBuilder buffer, CancellationToken cancellationToken = default)
    {
        var player = cmd.Player;

        if (string.IsNullOrWhiteSpace(cmd.Target))
        {
            throw new NotImplementedException(
                "When no target is specified for 'examine', just print the room's description");
        }
        
        if (cmd.Target is "me")
        {
            buffer.AppendLine(provider.ExamineSelf());

            var inventory = await player.QueryAsync(s => s.Inventory);
            
            var mine = inventory
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
        
        var current = await player.QueryAsync(s => s.CurrentLocation);

        var contents = await current.QueryAsync(s => s.Contents);

        if (contents.TryGetValue(cmd.Target, out var content))
        {
            var description = await content.QueryAsync(s => s.Description);

            buffer.AppendLine(description);
        }
    }
}