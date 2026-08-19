// File: src/Astrolabed.Dns/Models/EdnsOptionCode.cs
using System;

namespace Astrolabed.Dns.Models;

public sealed class EdnsOptionCode
{
    public ushort Code { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}

