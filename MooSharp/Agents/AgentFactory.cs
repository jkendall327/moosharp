using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MooSharp.Agents;

public class AgentFactory(
    ChannelWriter<GameInput> writer,
    TimeProvider clock,
    IOptions<AgentOptions> options,
    CommandReference commandReference,
    ILoggerFactory loggerFactory)
{
    public AgentBrain Build(AgentIdentity identity)
    {
        var availableCommands = commandReference.BuildHelpText();
        var logger = loggerFactory.CreateLogger($"{typeof(AgentBrain).FullName}[{identity.Name}]");

        return new(
            identity.Name,
            identity.Persona,
            identity.Source,
            availableCommands,
            writer,
            options,
            clock,
            logger,
            identity.Cooldown);
    }
}