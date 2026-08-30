namespace Astrolabed.Data.Models;

/// <summary>
/// Represents aggregated DNS event statistics grouped by hour of the day and blocked status.
/// </summary>
public sealed record DnsHourlyEventSummary
{
    /// <summary>
    /// Gets the hour of the day (0 through 23 UTC).
    /// </summary>
    public required int EventHour { get; init; }

    /// <summary>
    /// Gets a value indicating whether the events in this summary block were blocked (1) or allowed (0).
    /// </summary>
    public required bool IsBlocked { get; init; }

    /// <summary>
    /// Gets the total count of events for this specific hour and blocked state.
    /// </summary>

    public required long TotalEvents { get; init; }
}
