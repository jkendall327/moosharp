using System.Threading.Channels;

namespace MooSharp;

public interface IActorMessage<T>
{
    /// The context is the state object that the actor protects.
    Task Process(T context);
}

public abstract class Actor<TState> where TState : class
{
    private readonly Channel<IActorMessage<TState>> _mailbox;
    protected readonly TState State;

    protected Actor(TState state)
    {
        State = state;

        // Create an "unbounded" channel, meaning it can hold any number of messages.
        _mailbox = Channel.CreateUnbounded<IActorMessage<TState>>();

        // Start the long-running task that processes messages.
        Task.Run(ProcessMailboxAsync);
    }

    // The main loop for the actor. It runs forever, processing one message at a time.
    private async Task ProcessMailboxAsync()
    {
        await foreach (var message in _mailbox.Reader.ReadAllAsync())
        {
            try
            {
                await message.Process(State);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Actor for {State.GetType().Name} encountered an error: {ex.Message}");
            }
        }
    }

    public void Post(IActorMessage<TState> message)
    {
        _mailbox.Writer.TryWrite(message);
    }

    public Task<TResult> Ask<TResult>(IRequestMessage<TState, TResult> message)
    {
        Post(message);

        return message.GetResponseAsync();
    }
    
    public TResult QueryState<TResult>(Func<TState, TResult> query)
    {
        return query(State);
    }
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