using System;

using Astrolabed.Dns.Core;

namespace Astrolabed.Dns.RuleEngine;

/// <summary>
/// Defines the contract for high-performance DNS response caching.
/// </summary>
public interface IDnsCache : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this cache instance.
    /// </summary>
    Guid InstanceId { get; }

    /// <summary>
    /// Attempts to retrieve and patch a cached DNS response for the specified request context.
    /// </summary>
    /// <param name="context">The DNS request context containing query details and transaction state.</param>
    /// <param name="response">When this method returns, contains the patched DNS response byte array if found and valid; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a valid response was retrieved and patched; otherwise, <see langword="false"/>.</returns>
    bool TryGet(in DnsRequestContext context, out byte[]? response);

    /// <summary>
    /// Stores a DNS response in the cache with the specified Time-To-Live (TTL).
    /// </summary>
    /// <param name="context">The DNS request context associated with the response.</param>
    /// <param name="response">The raw DNS response payload to cache.</param>
    /// <param name="ttl">The duration for which the cached response remains valid.</param>
    void Store(in DnsRequestContext context, byte[] response, TimeSpan ttl);
}
