using System.Threading.Channels;

namespace MooSharp;

public interface IActorMessage
{
    /// The context is the state object that the actor protects.
    Task Process(object context);
}

public abstract class Actor<TState> where TState : class
{
    private readonly Channel<IActorMessage> _mailbox;
    protected readonly TState State;

    protected Actor(TState state)
    {
        State = state;

        // Create an "unbounded" channel, meaning it can hold any number of messages.
        _mailbox = Channel.CreateUnbounded<IActorMessage>();

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

    public void Post(IActorMessage message)
    {
        _mailbox.Writer.TryWrite(message);
    }

    public Task<TResult> Ask<TResult>(IRequestMessage<TResult> message)
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
public class ActionMessage : IActorMessage
{
    private readonly Func<object, Task> _action;
    public ActionMessage(Func<object, Task> action) => _action = action;
    public async Task Process(object context) => await _action(context);
}

// An interface for messages that need to return a value.
public interface IRequestMessage<TResult> : IActorMessage
{
    Task<TResult> GetResponseAsync();
}

// The implementation uses a TaskCompletionSource to bridge the async gap.
public class RequestMessage<TState, TResult> : IRequestMessage<TResult> where TState : class
{
    private readonly TaskCompletionSource<TResult> _tcs = new();
    private readonly Func<TState, Task<TResult>> _request;

    public RequestMessage(Func<TState, Task<TResult>> request) => _request = request;

    public async Task Process(object context)
    {
        try
        {
            var result = await _request((TState) context);
            _tcs.SetResult(result);
        }
        catch (Exception ex)
        {
            _tcs.SetException(ex);
        }
    }

    public Task<TResult> GetResponseAsync() => _tcs.Task;
}