using Astrolabed.Data.Pagination;

namespace Astrolabed.Dns.Cache;

/// <summary>
/// Defines high-performance in-memory caching operations for DNS response payloads.
/// </summary>
public interface IDnsCache
{

    /// <summary>
    /// Gets the number of items in the Cache
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Converts into a <see cref="PagedResult{CacheEntry}"/>.
    /// </summary>
    /// <param name="pageNumber">1-based page index.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A populated result</returns>
    PagedResult<KeyValuePair<string, CacheEntryView>> ToPagedResult(
        int pageNumber,
        int pageSize);

    /// <summary>
    /// Attempts to retrieve a cached DNS response payload for the specified domain and record type.
    /// </summary>
    /// <param name="domain">The target domain name to query.</param>
    /// <param name="qType">The numerical DNS record query type (e.g., 1 for A, 28 for AAAA).</param>
    /// <param name="payload">Outputs the cached DNS response payload bytes if found and valid.</param>
    /// <returns><c>true</c> if a valid non-expired cache entry exists; otherwise, <c>false</c>.</returns>
    bool TryGet(string domain, ushort qType, out ReadOnlyMemory<byte> payload);

    /// <summary>
    /// Attempts to retrieve a cached DNS response payload using a domain character span.
    /// </summary>
    /// <param name="domain">The target domain name span to query.</param>
    /// <param name="qType">The numerical DNS record query type (e.g., 1 for A, 28 for AAAA).</param>
    /// <param name="payload">Outputs the cached DNS response payload bytes if found and valid.</param>
    /// <returns><c>true</c> if a valid non-expired cache entry exists; otherwise, <c>false</c>.</returns>
    bool TryGet(ReadOnlySpan<char> domain, ushort qType, out ReadOnlyMemory<byte> payload) =>
        TryGet(domain.ToString(), qType, out payload);

    /// <summary>
    /// Stores a DNS response payload in the cache associated with the domain name and query type.
    /// </summary>
    /// <param name="domain">The target domain name.</param>
    /// <param name="qType">The numerical DNS record query type.</param>
    /// <param name="payload">The binary DNS response payload to store.</param>
    /// <param name="ttl">The time-to-live duration before the cached entry expires.</param>
    void Store(string domain, ushort qType, ReadOnlyMemory<byte> payload, TimeSpan ttl);

    /// <summary>
    /// Clears the Cache
    /// </summary>
    void Clear();
}
