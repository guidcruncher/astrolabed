// File: src/Astrolabed.Dns/Models/ExtendedDnsError.cs
namespace Astrolabed.Dns.Models;

/// <summary>
/// Encapsulates Extended DNS Error (EDE) details attached to an EDNS option payload (RFC 8914).
/// </summary>
public sealed class ExtendedDnsError
{
    /// <summary>
    /// Gets or sets the EDE informational purpose error code.
    /// </summary>
    public ExtendedDnsErrorCode InfoCode { get; set; }

    /// <summary>
    /// Gets or sets the optional human-readable UTF-8 diagnostic text.
    /// </summary>
    public string ExtraText { get; set; } = string.Empty;
}
