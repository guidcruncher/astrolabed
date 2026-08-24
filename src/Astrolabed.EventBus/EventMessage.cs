// File: src/Astrolabed.EventBus/EventMessage.cs
namespace Astrolabed.EventBus;

/// <summary>
/// Immutable generic container representing an event message envelope paired with a UTC dispatch timestamp.
/// </summary>
/// <typeparam name="T">The type of the event payload.</typeparam>
public sealed record EventMessage<T>
{
    /// <summary>
    /// Gets the event payload instance.
    /// </summary>
    public T Payload { get; }

    /// <summary>
    /// Gets the UTC timestamp indicating when the message envelope was generated.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventMessage{T}"/> record.
    /// </summary>
    /// <param name="payload">The message payload instance.</param>
    /// <param name="timestamp">The UTC timestamp associated with the event dispatch.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <c>null</c>.</exception>
    public EventMessage(T payload, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(payload);

        Payload = payload;
        Timestamp = timestamp;
    }

    /// <summary>
    /// Factory method to create an event message initialized with the timestamp from the specified or system clock provider.
    /// </summary>
    /// <param name="payload">The message payload instance.</param>
    /// <param name="timeProvider">Optional <see cref="TimeProvider"/> instance for obtaining current UTC timestamp.</param>
    /// <returns>A new <see cref="EventMessage{T}"/> instance wrapping the payload and dispatch timestamp.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <c>null</c>.</exception>
    public static EventMessage<T> Create(T payload, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(payload);

        TimeProvider provider = timeProvider ?? TimeProvider.System;
        return new EventMessage<T>(payload, provider.GetUtcNow());
    }
}
