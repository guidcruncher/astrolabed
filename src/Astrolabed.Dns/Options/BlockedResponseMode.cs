// File: src/Astrolabed.Dns/Options/DnsEngineOptions.cs
namespace Astrolabed.Dns.Options;

/// <summary>
/// Specifies the response strategy executed by the DNS engine when a query matches a blocking filter rule.
/// </summary>
public enum BlockedResponseMode
{
    /// <summary>
    /// Responds with a REFUSED (RCODE 5) response code indicating the query was explicitly rejected.
    /// </summary>
    Refused,

    /// <summary>
    /// Responds with a NODATA response code
    /// </summary>
    NoData,

    /// <summary>
    /// Responds with an NXDOMAIN (RCODE 3) response code indicating the requested domain name does not exist.
    /// </summary>
    NxDomain,

    /// <summary>
    /// Responds with a SERVFAIL (RCODE 2) response code indicating a server failure condition.
    /// </summary>
    ServFail,

    /// <summary>
    /// Responds with a successful record answer containing a zero IP address (0.0.0.0 for IPv4 or :: for IPv6).
    /// </summary>
    ZeroIp,

    /// <summary>
    /// Responds with a successful record answer containing a user-configured custom IP address (sinkhole/sink-server).
    /// </summary>
    CustomIp
}
