using System.Net;

using Astrolabed.Dns.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Upstream;

/// <summary>
/// Defines the supported transport protocol mechanisms for upstream DNS resolution queries.
/// </summary>
public enum TransportProtocol
{
    /// <summary>
    /// Standard UDP transport protocol (RFC 1035).
    /// </summary>
    Udp,

    /// <summary>
    /// Standard TCP transport protocol (RFC 1035).
    /// </summary>
    Tcp,

    /// <summary>
    /// DNS over HTTPS transport protocol (RFC 8484).
    /// </summary>
    Doh
}

/// <summary>
/// Dispatches DNS queries to corresponding protocol-specific upstream client implementations based on target server URIs.
/// </summary>
/// <param name="serviceProvider">The application service provider instance.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class UpstreamClientFactory(
    IServiceProvider serviceProvider,
    ILogger<UpstreamClientFactory> logger) : IUpstreamClientFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<UpstreamClientFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DnsWireMessage?> ExecuteQueryAsync(string targetServer, ReadOnlyMemory<byte> rawRequest, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetServer))
        {
            LogTargetServerNullOrEmpty(_logger);
            return null;
        }

        var (ipAddress, protocol) = ParseTargetServer(targetServer);
        if (ipAddress is null)
        {
            LogTargetServerParsingFailed(_logger, targetServer);
            return null;
        }

        IDnsUpstreamClient client = protocol switch
        {
            TransportProtocol.Tcp => _serviceProvider.GetRequiredService<TcpUpstreamDnsClient>(),
            TransportProtocol.Doh => _serviceProvider.GetRequiredService<DoHUpstreamDnsClient>(),
            _ => _serviceProvider.GetRequiredService<UdpUpstreamDnsClient>()
        };

        return await client.QueryAsync(ipAddress, rawRequest, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses an upstream target server string into an <see cref="IPAddress"/> and corresponding <see cref="TransportProtocol"/>.
    /// </summary>
    /// <param name="targetServer">The target server string (e.g., "1.1.1.1", "tcp://8.8.8.8:53", "https://1.1.1.1/dns-query").</param>
    /// <returns>A tuple containing the resolved IP address and transport protocol mode.</returns>
    public static (IPAddress? Address, TransportProtocol Protocol) ParseTargetServer(string targetServer)
    {
        if (string.IsNullOrWhiteSpace(targetServer))
        {
            return (null, TransportProtocol.Udp);
        }

        ReadOnlySpan<char> span = targetServer.AsSpan().Trim();

        if (span.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(targetServer, UriKind.Absolute, out Uri? uri) &&
                IPAddress.TryParse(uri.Host.Trim('[', ']'), out IPAddress? dohIp))
            {
                return (dohIp, TransportProtocol.Doh);
            }

            return (null, TransportProtocol.Doh);
        }

        TransportProtocol protocol = TransportProtocol.Udp;

        if (span.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        {
            protocol = TransportProtocol.Tcp;
            span = span[6..];
        }
        else if (span.StartsWith("udp://", StringComparison.OrdinalIgnoreCase))
        {
            protocol = TransportProtocol.Udp;
            span = span[6..];
        }

        // Handle path components if any exist
        int slashIdx = span.IndexOf('/');
        if (slashIdx >= 0)
        {
            span = span[..slashIdx];
        }

        // Handle IPv6 bracket notation [2001:db8::1]:53 or [2001:db8::1]
        if (span.StartsWith('['))
        {
            int closeBracketIdx = span.IndexOf(']');
            if (closeBracketIdx > 1)
            {
                ReadOnlySpan<char> ipSpan = span[1..closeBracketIdx];
                if (IPAddress.TryParse(ipSpan, out IPAddress? ipv6Address))
                {
                    return (ipv6Address, protocol);
                }
            }

            return (null, protocol);
        }

        // Handle IPv4 host:port splitting
        int lastColonIdx = span.LastIndexOf(':');
        if (lastColonIdx >= 0)
        {
            span = span[..lastColonIdx];
        }

        if (IPAddress.TryParse(span, out IPAddress? parsedIp))
        {
            return (parsedIp, protocol);
        }

        return (null, protocol);
    }

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Warning,
        Message = "Target server configuration string was null, empty, or white-space.")]
    private static partial void LogTargetServerNullOrEmpty(ILogger logger);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Warning,
        Message = "Failed to parse IP address and protocol from upstream server string '{TargetServer}'.")]
    private static partial void LogTargetServerParsingFailed(ILogger logger, string targetServer);
}
