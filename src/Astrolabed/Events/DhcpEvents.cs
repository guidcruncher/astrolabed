using System.Net;
using System.Net.NetworkInformation;

namespace Astrolabed.Events;

public sealed record DhcpLeaseAllocatedEvent(
    DateTimeOffset Timestamp,
    IPAddress ClientIp,
    PhysicalAddress Mac,
    string? ClientName,
    IPAddress ServerId,
    DateTimeOffset LeaseStart,
    DateTimeOffset LeaseExpiry)
    : EventRecord(Timestamp);

public sealed record DhcpLeaseReleasedEvent(
    DateTimeOffset Timestamp,
    PhysicalAddress Mac,
    IPAddress? ClientIp,
    string? ClientName)
    : EventRecord(Timestamp);

public sealed record DhcpNakEvent(
    DateTimeOffset Timestamp,
    PhysicalAddress Mac,
    IPAddress? RequestedIp,
    string Reason)
    : EventRecord(Timestamp);
