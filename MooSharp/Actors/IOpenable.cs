namespace MooSharp.Actors;

public interface IOpenable
{
    string Name { get; }
    bool IsOpen { get; set; }
    bool CanBeOpened { get; }
}

public interface ILockable
{
    string Name { get; }
    bool IsLocked { get; set; }
    string? KeyId { get; }
    bool CanBeLocked { get; }
}
