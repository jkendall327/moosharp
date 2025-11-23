using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace MooSharp.Agents;

public class AgentFactory(ChannelWriter<GameInput> writer, TimeProvider clock, IOptions<AgentOptions> options)
{
    public AgentBrain Build(string name, string persona, AgentSource source, TimeSpan? cooldown = null)
    {
        return new(name, persona, source, writer, options, clock, cooldown);
    }
}