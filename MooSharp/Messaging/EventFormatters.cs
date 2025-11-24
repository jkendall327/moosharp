using System.Text;

namespace MooSharp.Messaging;

public class SystemMessageEventFormatter : IGameEventFormatter<SystemMessageEvent>
{
    public string FormatForActor(SystemMessageEvent gameEvent) => gameEvent.Message;

    public string FormatForObserver(SystemMessageEvent gameEvent) => gameEvent.Message;
}

public class RoomDescriptionEventFormatter : IGameEventFormatter<RoomDescriptionEvent>
{
    public string FormatForActor(RoomDescriptionEvent gameEvent) => gameEvent.Description;

    public string FormatForObserver(RoomDescriptionEvent gameEvent) => gameEvent.Description;
}

public class ExitNotFoundEventFormatter : IGameEventFormatter<ExitNotFoundEvent>
{
    public string FormatForActor(ExitNotFoundEvent gameEvent) => "That exit doesn't exist.";

    public string FormatForObserver(ExitNotFoundEvent gameEvent) => "That exit doesn't exist.";
}

public class PlayerMovedEventFormatter : IGameEventFormatter<PlayerMovedEvent>
{
    public string FormatForActor(PlayerMovedEvent gameEvent) => $"You head to {gameEvent.Destination.Description}.";

    public string FormatForObserver(PlayerMovedEvent gameEvent) =>
        $"{gameEvent.Player.Username} moved towards {gameEvent.Destination.Description}.";
}

public class PlayerDepartedEventFormatter : IGameEventFormatter<PlayerDepartedEvent>
{
    public string FormatForActor(PlayerDepartedEvent gameEvent) =>
        $"You head out through {gameEvent.ExitName}.";

    public string FormatForObserver(PlayerDepartedEvent gameEvent) =>
        $"{gameEvent.Player.Username} went to {gameEvent.ExitName}";
}

public class PlayerArrivedEventFormatter : IGameEventFormatter<PlayerArrivedEvent>
{
    public string FormatForActor(PlayerArrivedEvent gameEvent) =>
        $"You arrived in {gameEvent.Destination.Description}.";

    public string FormatForObserver(PlayerArrivedEvent gameEvent) =>
        $"{gameEvent.Player.Username} arrived";
}

public class ItemNotFoundEventFormatter : IGameEventFormatter<ItemNotFoundEvent>
{
    public string FormatForActor(ItemNotFoundEvent gameEvent) => $"There is no {gameEvent.ItemName} here.";

    public string FormatForObserver(ItemNotFoundEvent gameEvent) => FormatForActor(gameEvent);
}

public class ItemTakenEventFormatter : IGameEventFormatter<ItemTakenEvent>
{
    public string FormatForActor(ItemTakenEvent gameEvent) => $"You take the {gameEvent.Item.Name}.";

    public string FormatForObserver(ItemTakenEvent gameEvent) => $"Someone takes the {gameEvent.Item.Name}.";
}

public class ItemAlreadyInPossessionEventFormatter : IGameEventFormatter<ItemAlreadyInPossessionEvent>
{
    public string FormatForActor(ItemAlreadyInPossessionEvent gameEvent) =>
        $"You already have the {gameEvent.Item.Name}.";

    public string FormatForObserver(ItemAlreadyInPossessionEvent gameEvent) =>
        $"Someone already has the {gameEvent.Item.Name}.";
}

public class ItemOwnedByOtherEventFormatter : IGameEventFormatter<ItemOwnedByOtherEvent>
{
    public string FormatForActor(ItemOwnedByOtherEvent gameEvent) =>
        $"{gameEvent.Owner.Username} already has the {gameEvent.Item.Name}!";

    public string FormatForObserver(ItemOwnedByOtherEvent gameEvent) =>
        $"{gameEvent.Owner.Username} already has the {gameEvent.Item.Name}!";
}

public class SelfExaminedEventFormatter : IGameEventFormatter<SelfExaminedEvent>
{
    public string FormatForActor(SelfExaminedEvent gameEvent)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You took a look at yourself. You're looking pretty good.");

        if (gameEvent.Inventory.Count > 0)
        {
            sb.AppendLine("You have:");

            foreach (var item in gameEvent.Inventory)
            {
                sb.AppendLine(item.Description);
            }
        }

        return sb.ToString().TrimEnd();
    }

    public string FormatForObserver(SelfExaminedEvent gameEvent) => "Someone seems to be checking themselves out.";
}

public class ObjectExaminedEventFormatter : IGameEventFormatter<ObjectExaminedEvent>
{
    public string FormatForActor(ObjectExaminedEvent gameEvent) => gameEvent.Item.Description;

    public string FormatForObserver(ObjectExaminedEvent gameEvent) => gameEvent.Item.Description;
}
