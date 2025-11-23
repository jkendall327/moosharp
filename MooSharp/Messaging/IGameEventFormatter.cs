namespace MooSharp.Messaging;

public interface IGameEventFormatter
{
    bool CanFormat(IGameEvent gameEvent);

    string FormatForActor(IGameEvent gameEvent);

    string FormatForObserver(IGameEvent gameEvent);
}

public interface IGameEventFormatter<in TEvent> : IGameEventFormatter where TEvent : IGameEvent
{
    string FormatForActor(TEvent gameEvent);

    string FormatForObserver(TEvent gameEvent);

    bool IGameEventFormatter.CanFormat(IGameEvent gameEvent) => gameEvent is TEvent;

    string IGameEventFormatter.FormatForActor(IGameEvent gameEvent) => FormatForActor((TEvent)gameEvent);

    string IGameEventFormatter.FormatForObserver(IGameEvent gameEvent) => FormatForObserver((TEvent)gameEvent);
}
