namespace Astrolabed.EventBus;

/// <summary>
/// Defines a non-generic metadata marker contract used to identify registered event message listeners 
/// and extract payload message types within dependency injection service containers.
/// </summary>
public interface IEventListenerMarker
{
    /// <summary>
    /// Gets the CLR <see cref="Type"/> of the payload message handled by the registered event listener.
    /// </summary>
    /// <value>
    /// A non-null <see cref="Type"/> instance corresponding to the event message payload.
    /// </value>
    Type MessageType { get; }
}
