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
    protected readonly TState _state;
    private readonly ILogger _logger;
    private readonly string _typeName;

    protected Actor(TState state, ILoggerFactory loggerFactory)
    {
        _state = state;
        _logger = loggerFactory.CreateLogger(GetType());

        _mailbox = Channel.CreateBounded<IActorMessage<TState>>(100);

        _typeName = state.GetType()
            .Name;

        // Start the long-running task that processes messages.
        Task.Run(ProcessMailboxAsync);
    }

    // The main loop for the actor. It runs forever, processing one message at a time.
    private async Task ProcessMailboxAsync()
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            {
                "ActorType", _typeName
            },
            {
                "State", _state.GetHashCode()
            }
        });

        _logger.LogInformation("Actor Loop STARTED");

        try
        {
            await foreach (var message in _mailbox.Reader.ReadAllAsync())
            {
                _logger.LogDebug("Processing message...");

                try
                {
                    await message.Process(_state);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while processing message");
                }

                _logger.LogDebug("Processed message");
            }
        }
        catch (Exception ex)
        {
            // This captures if the Channel reader itself crashes or the loop exits unexpectedly
            _logger.LogCritical(ex, "Actor Loop CRASHED");
        }
        finally
        {
            _logger.LogWarning("Actor Loop STOPPED");
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

    public override string? ToString() => _state.ToString();
}

/// A message that just performs an action and doesn't return anything.
public class ActionMessage<T> : IActorMessage<T>
{
    private readonly Func<T, Task> _action;
    public ActionMessage(Func<T, Task> action) => _action = action;
    public async Task Process(T context) => await _action(context);
}

// An interface for messages that need to return a value.
public interface IRequestMessage<in TState, TResult> : IActorMessage<TState>
{
    Task<TResult> GetResponseAsync();
}

// The implementation uses a TaskCompletionSource to bridge the async gap.
public class RequestMessage<TState, TResult> : IRequestMessage<TState, TResult> where TState : class
{
    /// <summary>
    /// We must run continuations asynchronously here to avoid potentially very unintuitive bugs.
    /// When this flag isn't set, continuations will be run synchronously, which means the thread which was
    /// meant to manage an actor's mailbox processing may instead be hijacked to handle long-running synchronous work.
    /// For example, since we use .Ask when setting up the World, the synchronous continuation there flowed out to
    /// Program.cs and app.Run() -- hijacking an actor's mailbox thread to run the entire Kestrel web server!
    /// Forcing asynchronous continuations saves us from that.
    /// An actor should not run a caller's code. It's too dangerous.
    /// </summary>
    private readonly TaskCompletionSource<TResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

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