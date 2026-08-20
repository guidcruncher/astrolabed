namespace Astrolabed.EventBus;

/// <summary>
/// Descriptor representing a registered message type for an event listener.
/// </summary>
public sealed class EventListenerDescriptor : IEventListenerMarker
{
    public EventListenerDescriptor(Type messageType)
    {
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
    }

    public Type MessageType { get; }
}
