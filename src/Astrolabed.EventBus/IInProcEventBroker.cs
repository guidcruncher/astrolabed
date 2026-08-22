namespace Astrolabed.EventBus;

/// <summary>
/// Contract for the central in-process event broker.
/// </summary>
public interface IInProcEventBroker
{
    /// <summary>
    /// Dispatches a message to all registered listeners in a fire-and-forget manner.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="payload">The message payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A ValueTask that completes as soon as the dispatch is enqueued.</returns>
    ValueTask PublishAsync<T>(T payload, CancellationToken cancellationToken = default) where T : notnull;

    /// <summary>
    /// Registers a delegate handler for a specific generic message type.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="handler">The callback to invoke when a message is dispatched.</param>
    /// <returns>An IDisposable subscription token to unregister the handler.</returns>
    IDisposable RegisterListener<T>(Func<EventMessage<T>, CancellationToken, ValueTask> handler) where T : notnull;
}
