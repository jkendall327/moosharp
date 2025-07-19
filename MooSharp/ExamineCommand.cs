using System.Text;

namespace MooSharp;

public class ExamineCommand : ICommand
{
    public required Player Player { get; set; }
    public required string Target { get; set; }
}

public class ExamineHandler(PlayerMultiplexer multiplexer) : IHandler<ExamineCommand>
{
    public async Task Handle(ExamineCommand cmd, StringBuilder buffer, CancellationToken cancellationToken = default)
    {
        var player = cmd.Player;

        if (cmd.Target is "me")
        {
            throw new NotImplementedException();
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}