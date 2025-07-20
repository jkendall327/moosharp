using System.Text;

namespace MooSharp;

public class ExamineCommand : ICommand
{
    public required PlayerActor Player { get; init; }
    public required string Target { get; init; }
}

public class ExamineHandler(StringProvider provider) : IHandler<ExamineCommand>
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
            
            var descriptions = inventory
                               .Select(s => s.Value.Description)
                               .ToList();

            if (descriptions.Any())
            {
                buffer.AppendLine("You have:");

                foreach (var se in descriptions)
                {
                    buffer.AppendLine(se);
                }
            }
        }
        
        var current = await player.QueryAsync(s => s.CurrentLocation);

        var contents = await current.QueryAsync(s => s.Contents);

        if (contents.TryGetValue(cmd.Target, out var content))
        {
            buffer.AppendLine(content.Description);
        }
    }
}