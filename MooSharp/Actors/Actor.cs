using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace MooSharp;

public interface IActorMessage<in T>
{
    /// The context is the state object that the actor protects.
    Task Process(T context);
}

public abstract class Actor<TState> where TState : class
{
    private readonly Channel<IActorMessage<TState>> _mailbox;
    protected readonly TState State;
    private readonly ILogger _logger;

    protected Actor(TState state, ILoggerFactory loggerFactory)
    {
        State = state;
        _logger = loggerFactory.CreateLogger(GetType());

        _mailbox = Channel.CreateBounded<IActorMessage<TState>>(100);

        // Start the long-running task that processes messages.
        Task.Run(ProcessMailboxAsync);
    }

    // The main loop for the actor. It runs forever, processing one message at a time.
    private async Task ProcessMailboxAsync()
    {
        await foreach (var message in _mailbox.Reader.ReadAllAsync())
        {
            _logger.LogInformation("Processing message (mailbox count: {Count}", _mailbox.Reader.Count);
            // Just allow any exceptions to bubble up and let callers handle them.
            await message.Process(State);
            
            _logger.LogInformation("Processed message");
        }
    }

    public void Post(IActorMessage<TState> message)
    {
        var posted = _mailbox.Writer.TryWrite(message);

        if (!posted)
        {
            _logger.LogWarning("Failed to post message to mailbox");
        }
    }

    public Task<TResult> Ask<TResult>(IRequestMessage<TState, TResult> message)
    {
        Post(message);

        return message.GetResponseAsync();
    }

    public async Task<TResult> QueryAsync<TResult>(Func<TState, TResult> func)
    {
        var message = new RequestMessage<TState, TResult>(state => Task.FromResult(func(state)));

        return await Ask(message);
    }

    public override string? ToString() => State.ToString();
}

/// A message that just performs an action and doesn't return anything.
public class ActionMessage<T> : IActorMessage<T>
{
    private readonly Func<T, Task> _action;
    public ActionMessage(Func<T, Task> action) => _action = action;
    public async Task Process(T context) => await _action(context);
}

// An interface for messages that need to return a value.
public interface IRequestMessage<TState, TResult> : IActorMessage<TState>
{
    Task<TResult> GetResponseAsync();
}

// The implementation uses a TaskCompletionSource to bridge the async gap.
public class RequestMessage<TState, TResult> : IRequestMessage<TState, TResult> where TState : class
{
    private readonly TaskCompletionSource<TResult> _tcs = new();
    private readonly Func<TState, Task<TResult>> _request;

    public RequestMessage(Func<TState, Task<TResult>> request) => _request = request;

    public async Task Process(TState context)
    {
        try
        {
            var result = await _request(context);
            _tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            _tcs.SetException(ex);
        }
    }

    public Task<TResult> GetResponseAsync() => _tcs.Task;
}