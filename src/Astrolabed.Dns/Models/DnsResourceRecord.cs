// File: src/Astrolabed.Dns/Models/DnsResourceRecord.cs
using System.Net;

namespace Astrolabed.Dns.Models;

/// <summary>
/// Represents a DNS Resource Record (RR) contained within the Answer, Authority, or Additional sections of a DNS packet.
/// </summary>
public sealed class DnsResourceRecord
{
    /// <summary>
    /// Gets or sets the domain name to which this resource record pertains.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DNS resource record type.
    /// </summary>
    public DnsType Type { get; set; }

    /// <summary>
    /// Gets or sets the class code for the record (typically 1 for IN / Internet).
    /// </summary>
    public ushort Class { get; set; } = 1;

    /// <summary>
    /// Gets or sets the time-to-live (TTL) interval in seconds that the record may be cached.
    /// </summary>
    public uint Ttl { get; set; }

    /// <summary>
    /// Gets or sets the raw binary payload (RDATA) of the resource record.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the pre-parsed IP address if the record represents an address type (e.g., A or AAAA).
    /// </summary>
    public IPAddress? ParsedIp { get; set; }
}
