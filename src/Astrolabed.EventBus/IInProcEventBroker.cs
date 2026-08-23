namespace Astrolabed.EventBus;

/// <summary>
/// Defines the core contract for the central in-process event broker, enabling decoupled pub/sub messaging across application components.
/// </summary>
public interface IInProcEventBroker
{
    /// <summary>
    /// Dispatches an event payload to all active registered listeners asynchronously.
    /// </summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="payload">The event message payload instance to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during message dispatch.</param>
    /// <returns>A <see cref="ValueTask"/> that completes as soon as the event message has been published or queued.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is null.</exception>
    ValueTask PublishAsync<T>(T payload, CancellationToken cancellationToken = default) where T : notnull;

    /// <summary>
    /// Registers an asynchronous handler delegate to receive dispatched event messages of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The event payload type to subscribe to.</typeparam>
    /// <param name="handler">The asynchronous callback invoked when an event message of type <typeparamref name="T"/> is published.</param>
    /// <returns>A <see cref="BrokerSubscriptionToken"/> that handles unregistering the listener when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
    BrokerSubscriptionToken RegisterListener<T>(Func<EventMessage<T>, CancellationToken, ValueTask> handler) where T : notnull;
}
