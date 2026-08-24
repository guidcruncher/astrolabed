// File: src/Astrolabed.EventBus/Options/EventBusOptions.cs
namespace Astrolabed.EventBus.Options;

/// <summary>
/// Configuration options for the in-process event bus.
/// </summary>
public sealed class EventBusOptions
{
    /// <summary>
    /// The configuration section name used to bind event bus options.
    /// </summary>
    public const string SectionName = "EventBus";

    /// <summary>
    /// Gets or sets a value indicating whether exceptions thrown by listeners are caught and logged instead of bubbling up.
    /// Defaults to <c>true</c> for fire-and-forget resiliency.
    /// </summary>
    public bool SuppressListenerExceptions { get; set; } = true;
}
