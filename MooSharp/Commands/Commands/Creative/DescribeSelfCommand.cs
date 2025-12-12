using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Presentation;
using MooSharp.Data;
using MooSharp.Data.Players;

namespace MooSharp.Commands.Commands.Creative;

public class DescribeSelfCommand : CommandBase<DescribeSelfCommand>
{
    public required Player Player { get; init; }
    public required string NewDescription { get; init; }
}

public class DescribeSelfHandler(IPlayerRepository repo) : IHandler<DescribeSelfCommand>
{
    public async Task<CommandResult> Handle(DescribeSelfCommand cmd, CancellationToken ct = default)
    {
        var result = new CommandResult();

        if (string.IsNullOrWhiteSpace(cmd.NewDescription))
        {
            result.Add(cmd.Player, new SystemMessageEvent("Usage: @describe me <description>."));

            return result;
        }

        cmd.Player.Description = cmd.NewDescription;

        await repo.UpdatePlayerDescriptionAsync(cmd.Player.Id.Value, cmd.NewDescription, WriteType.Immediate, ct);

        result.Add(cmd.Player, new SystemMessageEvent($"You change your description to: {cmd.NewDescription}"));

        return result;
    }
}
