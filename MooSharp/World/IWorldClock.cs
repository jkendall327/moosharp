namespace MooSharp.World;

public interface IWorldClock
{
    Task TriggerTickAsync(CancellationToken cancellationToken);
}
