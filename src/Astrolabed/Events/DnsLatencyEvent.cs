namespace Astrolabed.Events;

public sealed record DnsLatencyEvent(double Seconds, DateTimeOffset Timestamp) : EventRecord(Timestamp)
{
    public DnsLatencyEvent(double seconds) : this(seconds, DateTimeOffset.UtcNow) { }
}
