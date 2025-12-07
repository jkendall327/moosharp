namespace MooSharp.Actors.Objects;

[Flags]
public enum ObjectFlags
{
    None = 0,
    Container = 1 << 1,
    Openable = 1 << 2,
    Open = 1 << 3,
    Lockable = 1 << 4,
    Locked = 1 << 5,
    Scenery = 1 << 6,
    LightSource = 1 << 7,
    Writeable = 1 << 8
}