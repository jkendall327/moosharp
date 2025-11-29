namespace MooSharp.Agents;

/// <summary>
/// Runtime configuration for an agent.
/// </summary>
public record AgentCreationBundle(
    string Name,
    string Persona,
    AgentSource Source,
    TimeSpan VolitionCooldown,
    TimeSpan ActionCooldown);