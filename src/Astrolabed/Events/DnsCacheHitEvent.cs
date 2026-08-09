namespace Astrolabed.Events;

public sealed record DnsCacheHitEvent() : EventRecord(DateTime.UtcNow);

