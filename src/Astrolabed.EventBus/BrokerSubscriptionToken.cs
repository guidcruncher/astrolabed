namespace Astrolabed.EventBus;

/// <summary>
/// Thread-safe token that handles unregistering a listener when disposed.
/// </summary>
public sealed class BrokerSubscriptionToken : IDisposable
{
    private Action? _unsubscribe;

    public BrokerSubscriptionToken(Action unsubscribe)
    {
        _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
    }

    public void Dispose()
    {
        var action = Interlocked.Exchange(ref _unsubscribe, null);
        action?.Invoke();
    }
}
