using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace MooSharp.Agents;

public class AgentFactory(
    ChannelWriter<GameInput> writer,
    TimeProvider clock,
    IOptions<AgentOptions> options,
    CommandReference commandReference)
{
    public AgentBrain Build(AgentIdentity identity, CancellationToken cancellationToken)
    {
        var availableCommands = commandReference.BuildHelpText();

        return new(
            identity.Name,
            identity.Persona,
            identity.Source,
            availableCommands,
            writer,
            options,
            clock,
            identity.Cooldown,
            cancellationToken);
    }
}