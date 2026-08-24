using System.Net;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Defines asynchronous loading and parsing operations for local and remote hosts file formats.
/// </summary>
public interface IHostsFileReader
{
    /// <summary>
    /// Asynchronously fetches and parses hosts file contents into a hostname-to-IP-addresses map.
    /// </summary>
    /// <param name="sourceLocation">The HTTP/HTTPS URL or local filesystem path (including file:// URIs) pointing to the hosts file.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only dictionary mapping normalized hostnames to their associated IP addresses.</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<IPAddress>>> ReadHostsAsync(string sourceLocation, CancellationToken ct = default);
}
