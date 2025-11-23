namespace MooSharp.Agents;

public class AgentIdentity
{
    public required string Name { get; set; }
    public required string Persona { get; set; }
    public required AgentSource Source { get; set; }
    public TimeSpan? Cooldown { get; set; }
}