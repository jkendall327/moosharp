using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace MooSharp.Agents;

public class AgentFactory(ChannelWriter<GameInput> writer, TimeProvider clock, IOptions<AgentOptions> options)
{
    public AgentBrain Build(AgentIdentity identity)
    {
        return new(identity.Name, identity.Persona, identity.Source, writer, options, clock, identity.Cooldown);
    }
}