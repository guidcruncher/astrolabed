using System.Net;

namespace Astrolabed.Data.Entities;

public sealed record DnsResponseEventDto(
    DateTimeOffset Timestamp,
    IPAddress ClientIp,
    string? ClientName,
    string QueryName,
    string QueryType,
    string Status,
    IPAddress? ResponseIp,
    long TimestampEpoch,
    bool IsBlocked);
