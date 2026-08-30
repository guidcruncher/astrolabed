namespace Astrolabed.Data.Models;

/// <summary>
/// Database entity for mapping hourly conditional aggregations.
/// </summary>
internal sealed class DnsHourlyEventSummaryEntity
{
    public int EventHour { get; set; }

    public int Blocked { get; set; }

    public int Allowed { get; set; }

    public DnsHourlyEventSummary ToDomain()
    {
        return new DnsHourlyEventSummary
        {
            EventHour = EventHour,
            Blocked = Blocked,
            Allowed = Allowed
        };
    }
}
