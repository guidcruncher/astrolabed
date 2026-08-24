namespace Astrolabed.EventBus;

/// <summary>
/// Descriptor representing a registered message type for an event listener within the message broker pipeline.
/// </summary>
/// <param name="messageType">The underlying CLR <see cref="Type"/> of the event message payload.</param>
public sealed class EventListenerDescriptor(Type messageType) : IEventListenerMarker, IEquatable<EventListenerDescriptor>
{
    /// <summary>
    /// Gets the underlying CLR <see cref="Type"/> of the event message payload handled by this listener.
    /// </summary>
    public Type MessageType { get; } = messageType ?? throw new ArgumentNullException(nameof(messageType));

    /// <inheritdoc />
    public bool Equals(EventListenerDescriptor? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return MessageType == other.MessageType;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as EventListenerDescriptor);

    /// <inheritdoc />
    public override int GetHashCode() => MessageType.GetHashCode();

    /// <summary>
    /// Compares two <see cref="EventListenerDescriptor"/> instances for equality.
    /// </summary>
    public static bool operator ==(EventListenerDescriptor? left, EventListenerDescriptor? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="EventListenerDescriptor"/> instances for inequality.
    /// </summary>
    public static bool operator !=(EventListenerDescriptor? left, EventListenerDescriptor? right) => !(left == right);
}
