using System.Net;

using Astrolabed.Dns.Core;

namespace Astrolabed.Events;

public sealed record DnsQueryEvent(
    DateTimeOffset Timestamp,
    IPAddress ClientIp,
    string? ClientName,
    string QueryName,
    string QueryType)
    : EventRecord(Timestamp);

public sealed record DnsResponseEvent(
    DateTimeOffset Timestamp,
    IPAddress ClientIp,
    string? ClientName,
    string QueryName,
    string QueryType,
    DnsResponseCode Status,
    IPAddress? ResponseIp,
    long TimestampEpoch,
    bool IsBlocked)
    : EventRecord(Timestamp);

public sealed record DnsUpstreamLatencyEvent(
    DateTimeOffset Timestamp,
    string UpstreamName,
    TimeSpan Duration,
    bool Success)
    : EventRecord(Timestamp);
