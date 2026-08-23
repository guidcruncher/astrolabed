using System.Collections.Frozen;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Models;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Resolves hostnames against loaded local hosts file records using zero-allocation lookups.
/// </summary>
public sealed partial class HostRecordResolver : IHostRecordResolver
{
    /// <summary>
    /// An immutable, highly optimized lookup dictionary mapping normalized hostnames to their hosts file entries.
    /// </summary>
    private readonly FrozenDictionary<string, HostsEntry> _hostsLookup;

    /// <summary>
    /// The logger instance used for diagnostics and tracing lookup operations.
    /// </summary>
    private readonly ILogger<HostRecordResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostRecordResolver"/> class with the provided hosts file entries and logger.
    /// </summary>
    /// <param name="hostsEntries">Read-only collection of loaded hosts file entries.</param>
    /// <param name="logger">Structured logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hostsEntries"/> or <paramref name="logger"/> is <c>null</c>.</exception>
    public HostRecordResolver(IReadOnlyList<HostsEntry> hostsEntries, ILogger<HostRecordResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(hostsEntries);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Index entries into a frozen dictionary for O(1) case-insensitive lookups
        var dictionary = new Dictionary<string, HostsEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (HostsEntry entry in hostsEntries)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Hostname))
            {
                string key = entry.Hostname.Trim().TrimEnd('.');
                dictionary.TryAdd(key, entry);
            }
        }

        _hostsLookup = dictionary.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool TryResolveHost(string domain, DnsType recordType, out IPAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        string normalizedDomain = domain.Trim().TrimEnd('.');
        LogSearchingHosts(_logger, normalizedDomain);

        if (!_hostsLookup.TryGetValue(normalizedDomain, out HostsEntry? match) ||
            match.Addresses is null ||
            match.Addresses.Count == 0)
        {
            LogHostNotFound(_logger, normalizedDomain);
            return false;
        }

        // Match IP family against requested query record type
        address = recordType switch
        {
            DnsType.A => match.Addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork),
            DnsType.AAAA => match.Addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetworkV6),
            _ => null
        };

        return address != null;
    }

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Information,
        Message = "Looking in hosts for {NormalizedDomain}")]
    private static partial void LogSearchingHosts(ILogger logger, string normalizedDomain);

    [LoggerMessage(
        EventId = 302,
        Level = LogLevel.Warning,
        Message = "Domain {NormalizedDomain} not found in Hosts")]
    private static partial void LogHostNotFound(ILogger logger, string normalizedDomain);
}
