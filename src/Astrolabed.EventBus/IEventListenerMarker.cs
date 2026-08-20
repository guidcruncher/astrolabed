namespace Astrolabed.EventBus;

/// <summary>
/// Marker interface used to identify registered event message types in DI.
/// </summary>
public interface IEventListenerMarker
{
    /// <summary>
    /// Gets the payload message type handled by a registered listener.
    /// </summary>
    Type MessageType { get; }
}
