// File: src/Astrolabed.Dns/Models/DnsResourceRecord.cs
using System.Net;

namespace Astrolabed.Dns.Models;

public sealed class DnsResourceRecord
{
    public string Name { get; set; } = string.Empty;
    public DnsType Type { get; set; }
    public ushort Class { get; set; } = 1;
    public uint Ttl { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public IPAddress? ParsedIp { get; set; }
}
