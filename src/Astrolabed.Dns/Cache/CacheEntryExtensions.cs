using Astrolabed.Dns.Models;
using Astrolabed.Dns.Serialization;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Cache;

/// <summary>
/// Provides extension methods for converting binary cache entries to parsed DNS wire message cache entries.
/// </summary>
public static class CacheEntryExtensions
{
    /// <summary>
    /// Attempts to parse the binary DNS payload within a <see cref="CacheEntry"/> into a parsed <see cref="DnsWireMessage"/> cache entry.
    /// </summary>
    /// <param name="binaryEntry">The raw binary cache entry to convert.</param>
    /// <param name="parsedEntry">When this method returns <see langword="true"/>, contains the parsed cache entry; otherwise, <see langword="null"/>.</param>
    /// <param name="logger">An optional logger instance to record parsing failures.</param>
    /// <returns><see langword="true"/> if the payload was successfully parsed into a <see cref="DnsWireMessage"/>; otherwise, <see langword="false"/>.</returns>
    public static bool TryToMessageEntry(
        this CacheEntry binaryEntry,
        out CacheEntryView? parsedEntry,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(binaryEntry);

        if (DnsWireParser.TryParse(binaryEntry.Payload.Span, out DnsWireMessage message))
        {
            parsedEntry = new CacheEntryView(message, binaryEntry.ExpiresAt);
            return true;
        }

        logger?.LogWarning("Failed to parse DNS wire message payload from cache entry expiring at {ExpiresAt}.", binaryEntry.ExpiresAt);
        parsedEntry = null;
        return false;
    }
}
