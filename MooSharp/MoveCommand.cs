namespace MooSharp;

public class MoveCommand
{
    public required string TargetExit { get; set; }

    public string BroadcastMessage(Player player) => $"{player.Username} went to {TargetExit}";
}