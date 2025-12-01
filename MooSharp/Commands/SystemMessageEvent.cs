using MooSharp.Messaging;

namespace MooSharp.Commands;

public record SystemMessageEvent(string Message) : IGameEvent;

public class SystemMessageEventFormatter : IGameEventFormatter<SystemMessageEvent>
{
    public string FormatForActor(SystemMessageEvent gameEvent) => gameEvent.Message;

    public string FormatForObserver(SystemMessageEvent gameEvent) => gameEvent.Message;
}
