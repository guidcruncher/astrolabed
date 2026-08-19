// File: src/Astrolabed.Dns/Models/EdnsOptions.cs
using System.Collections.Generic;

namespace Astrolabed.Dns.Models;

public sealed class EdnsOptions
{
    public ushort UdpPayloadSize { get; set; } = 4096;
    public byte ExtendedRCode { get; set; }
    public byte Version { get; set; }
    public bool DnssecOk { get; set; }
    public List<EdnsOptionCode> Options { get; } = new();
}
