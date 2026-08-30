namespace Astrolabed.Data.Models;

/// <summary>
/// Database representation for mapped DnsHourlyEventSummary query execution.
/// </summary>
internal sealed class DnsHourlyEventSummaryEntity
{
    public int EventHour { get; set; }

    public int Blocked { get; set; }

    public long TotalEvents { get; set; }

    public DnsHourlyEventSummary ToDomain()
    {
        return new DnsHourlyEventSummary
        {
            EventHour = EventHour,
            IsBlocked = Blocked != 0,
            TotalEvents = TotalEvents
        };
    }
}
