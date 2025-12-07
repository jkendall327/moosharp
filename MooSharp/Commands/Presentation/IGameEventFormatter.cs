namespace MooSharp.Commands.Presentation;

public interface IGameEventFormatter
{
    bool CanFormat(IGameEvent gameEvent);

    string FormatForActor(IGameEvent gameEvent);

    /// <summary>
    /// Formats the event for displaying it to external observers.
    /// This may be null for events where it doesn't make sense for observers to see them.
    /// </summary>
    string? FormatForObserver(IGameEvent gameEvent);
}

public interface IGameEventFormatter<in TEvent> : IGameEventFormatter where TEvent : IGameEvent
{
    string FormatForActor(TEvent gameEvent);

    string? FormatForObserver(TEvent gameEvent);

    bool IGameEventFormatter.CanFormat(IGameEvent gameEvent) => gameEvent is TEvent;

    string IGameEventFormatter.FormatForActor(IGameEvent gameEvent) => FormatForActor((TEvent)gameEvent);

    string? IGameEventFormatter.FormatForObserver(IGameEvent gameEvent) => FormatForObserver((TEvent)gameEvent);
}
