using System.Net;

namespace Astrolabed.Dns.Models;

/// <summary>
/// Encapsulates execution tracking context and client metadata for an individual incoming DNS query operation.
/// </summary>
public sealed class DnsContext
{
    /// <summary>
    /// Gets the unique Version 7 GUID assigned to trace and correlate request lifetime events.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the client IP address that originated the DNS query.
    /// </summary>
    public IPAddress ClientIp { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsContext"/> class for the specified client IP.
    /// </summary>
    /// <param name="clientIp">The remote client origin IP address.</param>
    public DnsContext(IPAddress clientIp)
    {
        Id = Guid.CreateVersion7();
        ClientIp = clientIp;
    }
}
