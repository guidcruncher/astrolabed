namespace Astrolabed.Events;

public sealed record NtpSyncEvent(
    DateTimeOffset Timestamp,
    string Server,
    string ClientName, 
    string ClientIp,
    TimeSpan Offset,
    TimeSpan Delay,
    bool Success)
    : EventRecord(Timestamp);

public sealed record NtpOffsetEvent(
    DateTimeOffset Timestamp,
    string Peer,
    double OffsetSeconds)
    : EventRecord(Timestamp);
