using System.Net;

namespace Astrolabed.Events;

public sealed record NtpSyncEvent(
    DateTime Timestamp,
    IPAddress ClientIp,
    string? ClientName,
    TimeSpan Offset,
    bool Success)
    : EventRecord(Timestamp);
