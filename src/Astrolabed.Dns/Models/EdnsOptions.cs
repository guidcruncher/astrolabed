// File: src/Astrolabed.Dns/Models/EdnsOptions.cs
namespace Astrolabed.Dns.Models;

/// <summary>
/// Represents Extension Mechanisms for DNS (EDNS0) parameters and option fields (RFC 6891).
/// </summary>
public sealed class EdnsOptions
{
    /// <summary>
    /// Gets or sets the maximum UDP payload size in bytes that the sender can reassemble.
    /// </summary>
    public ushort UdpPayloadSize { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the upper 8 bits of the extended 12-bit RCODE.
    /// </summary>
    public byte ExtendedRCode { get; set; }

    /// <summary>
    /// Gets or sets the EDNS implementation version level.
    /// </summary>
    public byte Version { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client supports DNSSEC validation (DO bit).
    /// </summary>
    public bool DnssecOk { get; set; }

    /// <summary>
    /// Gets the list of attached EDNS option key-value pairs.
    /// </summary>
    public List<EdnsOptionCode> Options { get; } = new();
}
