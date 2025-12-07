namespace MooSharp.Features.WorldClock;

public interface IWorldClock
{
    Task TriggerTickAsync(CancellationToken cancellationToken);
}
