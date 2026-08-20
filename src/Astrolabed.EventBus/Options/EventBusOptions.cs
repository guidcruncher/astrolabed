namespace Astrolabed.EventBus.Options;

/// <summary>
/// Configuration options for the in-process event bus.
/// </summary>
public sealed class EventBusOptions
{
    /// <summary>
    /// Gets or sets whether exceptions thrown by listeners are caught and logged instead of bubbling up.
    /// Defaults to true for fire-and-forget resiliency.
    /// </summary>
    public bool SuppressListenerExceptions { get; set; } = true;
}
