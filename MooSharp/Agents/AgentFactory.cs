using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Messaging;

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

        var bundle = new AgentCreationBundle(identity.Name, identity.Persona, identity.Source, volition, cooldown);
        var core = new AgentCore(bundle, promptProvider, responseProvider, clock, options, logger);

        var connection = new AgentPlayerConnection();

        var brain = new AgentBrain(core, connection, writer, clock, options);

        return brain;
    }
}