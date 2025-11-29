using System.Threading;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MooSharp.Agents;

public class AgentFactory(
    ChannelWriter<GameInput> writer,
    TimeProvider clock,
    IOptions<AgentOptions> options,
    IAgentPromptProvider promptProvider,
    IAgentResponseProvider responseProvider,
    ILoggerFactory loggerFactory)
{
    public AgentBrain Build(AgentIdentity identity)
    {
        var logger = loggerFactory.CreateLogger($"{typeof(AgentBrain).FullName}[{identity.Name}]");

        var cooldown = identity.Cooldown ?? options.Value.DefaultActionCooldown;
        var volition = TimeSpan.FromMinutes(1);

        var bundle = new AgentCreationBundle(identity.Name, identity.Persona, identity.Source, cooldown, volition);

        return new(bundle, writer, promptProvider, clock, responseProvider, options, logger);
    }
}