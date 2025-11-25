using MooSharp.Messaging;

namespace MooSharp.Agents;

public record AgentThinkingEvent(Player Player) : IGameEvent;

public class AgentThinkingEventFormatter : IGameEventFormatter<AgentThinkingEvent>
{
    public string FormatForActor(AgentThinkingEvent gameEvent) => $"{gameEvent.Player.Username} is thinking...";

    public string? FormatForObserver(AgentThinkingEvent gameEvent) => FormatForActor(gameEvent);
}
