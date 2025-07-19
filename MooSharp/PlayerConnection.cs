using System.Text;

namespace MooSharp;

public interface IPlayerConnection
{
    Guid Id { get; set; }
    Player Player { get; }
    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
    Task SendMessageAsync(StringBuilder message, CancellationToken cancellationToken = default);
    Task<string?> GetStringAsync(CancellationToken cancellationToken = default);
}

public class StreamBasedPlayerConnection : IPlayerConnection
{
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Player Player { get; private set; }

    public StreamBasedPlayerConnection(Stream stream, Player player)
    {
        Player = player;

        _reader = new(stream);

        _writer = new(stream)
        {
            AutoFlush = true
        };
    }

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        await _writer.WriteLineAsync(message);
    }

    public async Task SendMessageAsync(StringBuilder message, CancellationToken cancellationToken = default)
    {
        await _writer.WriteLineAsync(message, cancellationToken);
    }

    public async Task<string?> GetStringAsync(CancellationToken cancellationToken = default)
    {
        var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        return await _reader.ReadLineAsync(linked.Token);
    }
}