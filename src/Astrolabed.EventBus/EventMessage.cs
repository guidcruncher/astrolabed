namespace Astrolabed.EventBus;

/// <summary>
/// Immutable generic container representing an event message with a dispatch timestamp.
/// </summary>
/// <typeparam name="T">The type of the event payload.</typeparam>
public sealed record EventMessage<T>(T Payload, DateTimeOffset Timestamp)
{
    /// <summary>
    /// Factory method to create an event message initialized with the current UTC timestamp.
    /// </summary>
    /// <param name="payload">The message payload instance.</param>
    /// <returns>A new EventMessage instance containing the payload and timestamp.</returns>
    public static EventMessage<T> Create(T payload) => new(payload, DateTimeOffset.UtcNow);
}
