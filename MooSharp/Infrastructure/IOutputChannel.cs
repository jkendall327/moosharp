namespace MooSharp.Infrastructure;

/// <summary>
/// Represents a pipe to send data back to a connected user (or agent).
/// </summary>
public interface IOutputChannel
{
    Task WriteOutputAsync(string message, CancellationToken ct = default);
}
