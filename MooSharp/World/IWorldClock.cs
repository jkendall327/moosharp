namespace MooSharp;

public interface IWorldClock
{
    Task TriggerTickAsync(CancellationToken cancellationToken);
}
