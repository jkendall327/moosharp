namespace MooSharp;

public record GameInput(string ConnectionId, InputCommand Command);

public abstract class InputCommand;

public class RegisterCommand : InputCommand
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class LoginCommand : InputCommand
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class WorldCommand : InputCommand
{
    public required string Command { get; set; }
}