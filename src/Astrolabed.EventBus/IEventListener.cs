namespace Astrolabed.EventBus;

/// <summary>
/// Interface for listening to generic event messages.
/// </summary>
/// <typeparam name="T">The type of the event payload.</typeparam>
public interface IEventListener<T>
{
    /// <summary>
    /// Handles the incoming event message asynchronously.
    /// </summary>
    /// <param name="message">The generic message container holding payload and timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A ValueTask representing the asynchronous handling operation.</returns>
    ValueTask HandleAsync(EventMessage<T> message, CancellationToken cancellationToken);
}
