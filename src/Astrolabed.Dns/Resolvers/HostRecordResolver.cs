// File: src/Astrolabed.Dns/Resolvers/HostRecordResolver.cs
using System.Collections.Frozen;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Models;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Resolves hostnames against loaded local hosts file records using zero-allocation lookups against the active hosts manager snapshot.
/// </summary>
public sealed partial class HostRecordResolver : IHostRecordResolver
{
    /// <summary>
    /// The backing hosts manager instance providing the active snapshot dictionary.
    /// </summary>
    private readonly IHostsManager _hostsManager;

    /// <summary>
    /// The logger instance used for diagnostics and tracing lookup operations.
    /// </summary>
    private readonly ILogger<HostRecordResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostRecordResolver"/> class with the provided hosts manager and logger.
    /// </summary>
    /// <param name="hostsManager">Manager supplying active lookup snapshots.</param>
    /// <param name="logger">Structured logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hostsManager"/> or <paramref name="logger"/> is <c>null</c>.</exception>
    public HostRecordResolver(IHostsManager hostsManager, ILogger<HostRecordResolver> logger)
    {
        _hostsManager = hostsManager ?? throw new ArgumentNullException(nameof(hostsManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool TryResolveHost(string domain, DnsType recordType, out IPAddress? address)
    {
        address = null;

        FrozenDictionary<string, HostsEntry> lookup = _hostsManager.Lookup;

        if (lookup.Count == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        string normalizedDomain = domain.Trim().TrimEnd('.');
        LogSearchingHosts(_logger, normalizedDomain);

        if (!lookup.TryGetValue(normalizedDomain, out HostsEntry? match) ||
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
