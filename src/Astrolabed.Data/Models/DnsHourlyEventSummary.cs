namespace Astrolabed.Data.Models;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Represents aggregated DNS event statistics for a specific hour of the day.
/// </summary>
public sealed record DnsHourlyEventSummary
{
    /// <summary>
    /// Gets the hour of the day in UTC (0 through 23).
    /// </summary>
    /// <example>14</example>
    [Required]
    [Range(0, 23)]
    [DefaultValue(0)]
    public required int EventHour { get; init; }

    /// <summary>
    /// Gets the total count of blocked DNS events during this hour.
    /// </summary>
    /// <example>42</example>
    [Required]
    [DefaultValue(0)]
    public required int Blocked { get; init; }

    /// <summary>
    /// Gets the total count of allowed (non-blocked) DNS events during this hour.
    /// </summary>
    /// <example>1208</example>
    [Required]
    [DefaultValue(0)]
    public required int Allowed { get; init; }
}
