using System.Net;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Upstream;

/// <summary>
/// Defines a contract for sending DNS query wire messages to upstream resolution servers.
/// </summary>
public interface IDnsUpstreamClient
{
    /// <summary>
    /// Asynchronously queries an upstream DNS server with a raw DNS wire-format message.
    /// </summary>
    /// <param name="targetServer">The target IP address of the upstream DNS server.</param>
    /// <param name="rawRequest">The raw binary DNS query payload formatted according to RFC 1035.</param>
    /// <param name="ct">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the parsed <see cref="DnsWireMessage"/>
    /// if the operation succeeded and payload was valid; otherwise, <see langword="null"/>.
    /// </returns>
    Task<DnsWireMessage?> QueryAsync(IPAddress targetServer, ReadOnlyMemory<byte> rawRequest, CancellationToken ct = default);
}
