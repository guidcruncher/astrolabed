namespace Astrolabed.EventBus;

/// <summary>
/// Defines the asynchronous message handler contract for listening to event messages containing a payload of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The payload type encapsulated within the event message envelope.</typeparam>
public interface IEventListener<T> : IEventListenerMarker
{
    /// <summary>
    /// Asynchronously processes an incoming event message envelope.
    /// </summary>
    /// <param name="message">The generic event message container holding the message payload and metadata dispatch timestamp.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during message handling.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous message handling operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    ValueTask HandleAsync(
        EventMessage<T> message,
        CancellationToken cancellationToken = default);
}
