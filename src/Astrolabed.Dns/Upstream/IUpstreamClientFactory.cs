using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Upstream;

/// <summary>
/// Defines a factory and execution abstraction for dispatching DNS queries to upstream resolvers.
/// </summary>
public interface IUpstreamClientFactory
{
    /// <summary>
    /// Executes a DNS query wire message against a specified target upstream server or endpoint.
    /// </summary>
    /// <param name="targetServer">The upstream server host, IP address, or URI endpoint.</param>
    /// <param name="rawRequest">The raw binary DNS query payload formatted according to RFC 1035.</param>
    /// <param name="ct">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous query operation. The task result contains the parsed <see cref="DnsWireMessage"/>
    /// if the operation succeeded and response was valid; otherwise, <see langword="null"/>.
    /// </returns>
    Task<DnsWireMessage?> ExecuteQueryAsync(string targetServer, ReadOnlyMemory<byte> rawRequest, CancellationToken ct = default);
}
