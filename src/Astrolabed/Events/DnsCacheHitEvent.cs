namespace Astrolabed.Events;

public sealed record DnsCacheHitEvent(DateTimeOffset Timestamp) : EventRecord(Timestamp)
{
    public DnsCacheHitEvent() : this(DateTimeOffset.UtcNow) { }
}
