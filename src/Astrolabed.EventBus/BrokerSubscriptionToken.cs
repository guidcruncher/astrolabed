namespace Astrolabed.EventBus;

/// <summary>
/// Thread-safe token that handles unregistering an event listener when disposed.
/// Guarantees exact-once execution of the unsubscription callback across concurrent threads.
/// </summary>
/// <param name="unsubscribe">The delegate action to execute upon subscription cancellation.</param>
public sealed class BrokerSubscriptionToken(Func<ValueTask>? unsubscribe) : IDisposable, IAsyncDisposable
{
    private Func<ValueTask>? _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));

    /// <summary>
    /// Initializes a new instance of the <see cref="BrokerSubscriptionToken"/> class with a synchronous unsubscription action.
    /// </summary>
    /// <param name="unsubscribe">The synchronous delegate action to execute upon cancellation.</param>
    public BrokerSubscriptionToken(Action unsubscribe)
        : this(unsubscribe is null ? throw new ArgumentNullException(nameof(unsubscribe)) : () => { unsubscribe(); return ValueTask.CompletedTask; })
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Func<ValueTask>? action = Interlocked.Exchange(ref _unsubscribe, null);
        if (action is null)
        {
            return;
        }

        ValueTask task = action.Invoke();
        if (!task.IsCompletedSuccessfully)
        {
            task.AsTask().GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Func<ValueTask>? action = Interlocked.Exchange(ref _unsubscribe, null);
        if (action is not null)
        {
            await action.Invoke().ConfigureAwait(false);
        }
    }
}
