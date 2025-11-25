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
    public string FormatForActor(PlayerMovedEvent gameEvent) => gameEvent.Destination.EnterText;

    public string FormatForObserver(PlayerMovedEvent gameEvent) =>
        $"{gameEvent.Player.Username} arrives.";
}

public class PlayerDepartedEventFormatter : IGameEventFormatter<PlayerDepartedEvent>
{
    public string FormatForActor(PlayerDepartedEvent gameEvent) =>
        gameEvent.Origin.ExitText;

    public string FormatForObserver(PlayerDepartedEvent gameEvent) =>
        $"{gameEvent.Player.Username} leaves.";
}

public class PlayerArrivedEventFormatter : IGameEventFormatter<PlayerArrivedEvent>
{
    public string FormatForActor(PlayerArrivedEvent gameEvent) =>
        gameEvent.Destination.EnterText;

    public string FormatForObserver(PlayerArrivedEvent gameEvent) =>
        $"{gameEvent.Player.Username} arrives.";
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

public class ItemDroppedEventFormatter : IGameEventFormatter<ItemDroppedEvent>
{
    public string FormatForActor(ItemDroppedEvent gameEvent) => $"You drop the {gameEvent.Item.Name}.";

    public string FormatForObserver(ItemDroppedEvent gameEvent) =>
        $"{gameEvent.Player.Username} drops the {gameEvent.Item.Name}.";
}

public class ItemNotCarriedEventFormatter : IGameEventFormatter<ItemNotCarriedEvent>
{
    public string FormatForActor(ItemNotCarriedEvent gameEvent) => $"You aren't carrying a {gameEvent.ItemName}.";

    public string FormatForObserver(ItemNotCarriedEvent gameEvent) => FormatForActor(gameEvent);
}

public class ItemGivenEventFormatter : IGameEventFormatter<ItemGivenEvent>
{
    public string FormatForActor(ItemGivenEvent gameEvent) =>
        $"You give the {gameEvent.Item.Name} to {gameEvent.Recipient.Username}.";

    public string FormatForObserver(ItemGivenEvent gameEvent) =>
        $"{gameEvent.Sender.Username} gives the {gameEvent.Item.Name} to {gameEvent.Recipient.Username}.";
}

public class ItemReceivedEventFormatter : IGameEventFormatter<ItemReceivedEvent>
{
    public string FormatForActor(ItemReceivedEvent gameEvent) =>
        $"{gameEvent.Sender.Username} gives you the {gameEvent.Item.Name}.";

    public string FormatForObserver(ItemReceivedEvent gameEvent) =>
        $"{gameEvent.Sender.Username} gives the {gameEvent.Item.Name} to {gameEvent.Recipient.Username}.";
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

public class AmbiguousInputEventFormatter : IGameEventFormatter<AmbiguousInputEvent>
{
    public string FormatForActor(AmbiguousInputEvent evt)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Which '{evt.Input}' do you mean?");

        var i = 1;
        foreach (var candidate in evt.Candidates)
        {
            sb.AppendLine($"{i++}. {candidate.Name}");
        }

        sb.Append("Type the name and the number (e.g., 'sword 2').");
        return sb.ToString();
    }

    public string? FormatForObserver(AmbiguousInputEvent evt) => null;
}

public class PlayerSaidEventFormatter : IGameEventFormatter<PlayerSaidEvent>
{
    public string FormatForActor(PlayerSaidEvent gameEvent) => $"[{gameEvent.Player.Username}]: \"{gameEvent.Message}\"";
    public string FormatForObserver(PlayerSaidEvent gameEvent) => FormatForActor(gameEvent);
}

public class PlayerEmotedEventFormatter : IGameEventFormatter<PlayerEmotedEvent>
{
    public string FormatForActor(PlayerEmotedEvent gameEvent) =>
        $"{gameEvent.Player.Username} {gameEvent.Message}";

    public string FormatForObserver(PlayerEmotedEvent gameEvent) =>
        $"{gameEvent.Player.Username} {gameEvent.Message}";
}

public class WhisperEventFormatter : IGameEventFormatter<WhisperEvent>
{
    public string FormatForActor(WhisperEvent gameEvent) =>
        $"You whisper to {gameEvent.Recipient.Username}, \"{gameEvent.Message}\"";

    public string FormatForObserver(WhisperEvent gameEvent) =>
        $"{gameEvent.Sender.Username} whispers to you, \"{gameEvent.Message}\"";
}
