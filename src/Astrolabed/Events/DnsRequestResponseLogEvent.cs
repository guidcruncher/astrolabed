using System.Net;

namespace Astrolabed.Events;

public sealed record DnsRequestResponseLogEvent(
    DateTimeOffset Timestamp,
    IPAddress ClientIp,
    string? ClientName,
    string DnsQuestion,
    string? DnsAnswer,
    bool Blocked)
    : EventRecord(Timestamp);
