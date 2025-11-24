namespace MooSharp.Messaging;

public record SystemMessageEvent(string Message) : IGameEvent;

public record RoomDescriptionEvent(string Description) : IGameEvent;

public record ExitNotFoundEvent(string ExitName) : IGameEvent;

public record PlayerMovedEvent(Player Player, Room Destination) : IGameEvent;

public record PlayerDepartedEvent(Player Player, Room Origin, string ExitName) : IGameEvent;

public record PlayerArrivedEvent(Player Player, Room Destination) : IGameEvent;

public record ItemNotFoundEvent(string ItemName) : IGameEvent;

public record ItemTakenEvent(Object Item) : IGameEvent;

public record ItemAlreadyInPossessionEvent(Object Item) : IGameEvent;

public record ItemOwnedByOtherEvent(Object Item, Player Owner) : IGameEvent;

public record SelfExaminedEvent(Player Player, IReadOnlyCollection<Object> Inventory) : IGameEvent;

public record ObjectExaminedEvent(Object Item) : IGameEvent;
