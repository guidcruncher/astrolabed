// File: src/Astrolabed.Dns/Models/EdnsOptionCode.cs
namespace Astrolabed.Dns.Models;

/// <summary>
/// Represents an individual Extension Mechanisms for DNS (EDNS0) option code key-value pair.
/// </summary>
public sealed class EdnsOptionCode
{
    /// <summary>
    /// Gets or sets the assigned EDNS option code identifier.
    /// </summary>
    public ushort Code { get; set; }

    /// <summary>
    /// Gets or sets the raw binary payload associated with the EDNS option.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
