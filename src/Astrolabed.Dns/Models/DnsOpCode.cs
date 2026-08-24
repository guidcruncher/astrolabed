// File: src/Astrolabed.Dns/Models/DnsOpCode.cs
namespace Astrolabed.Dns.Models;

/// <summary>
/// Represents RFC 1035 and RFC 2136 standard 4-bit DNS Operation Codes (OPCODE) in header messages.
/// </summary>
public enum DnsOpCode : byte
{
    /// <summary>
    /// Standard query operation (RFC 1035).
    /// </summary>
    Query = 0,

    /// <summary>
    /// Inverse query operation (RFC 1035, Obsolete).
    /// </summary>
    IQuery = 1,

    /// <summary>
    /// Server status request operation (RFC 1035).
    /// </summary>
    Status = 2,

    /// <summary>
    /// Zone change notification operation (RFC 1996).
    /// </summary>
    Notify = 4,

    /// <summary>
    /// Dynamic DNS update operation (RFC 2136).
    /// </summary>
    Update = 5
}
