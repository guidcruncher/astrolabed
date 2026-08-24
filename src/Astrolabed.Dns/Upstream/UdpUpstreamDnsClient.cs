// File: src/Astrolabed.Dns/Upstream/UdpUpstreamDnsClient.cs
using System.Buffers;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Serialization;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Upstream;

/// <summary>
/// Provides RFC 1035 compliant UDP upstream DNS resolution using high-performance socket operations.
/// </summary>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class UdpUpstreamDnsClient(
    ILogger<UdpUpstreamDnsClient> logger) : IDnsUpstreamClient
{
    private const int DefaultDnsPort = 53;
    private const int TimeoutMilliseconds = 2000;
    private const int MaxUdpBufferLength = 4096;

    private readonly ILogger<UdpUpstreamDnsClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DnsWireMessage?> QueryAsync(IPAddress targetServer, ReadOnlyMemory<byte> rawRequest, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(targetServer);

        var upstreamEp = new IPEndPoint(targetServer, DefaultDnsPort);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeoutMilliseconds);

        using var socket = new Socket(targetServer.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            ReceiveTimeout = TimeoutMilliseconds,
            SendTimeout = TimeoutMilliseconds
        };

        try
        {
            await socket.SendToAsync(rawRequest, SocketFlags.None, upstreamEp, cts.Token).ConfigureAwait(false);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(MaxUdpBufferLength);
            try
            {
                EndPoint receiveEndPoint = targetServer.AddressFamily == AddressFamily.InterNetworkV6
                    ? new IPEndPoint(IPAddress.IPv6Any, 0)
                    : new IPEndPoint(IPAddress.Any, 0);

                SocketReceiveFromResult result = await socket.ReceiveFromAsync(
                    buffer.AsMemory(0, MaxUdpBufferLength),
                    SocketFlags.None,
                    receiveEndPoint,
                    cts.Token).ConfigureAwait(false);

                if (result.ReceivedBytes == 0)
                {
                    LogEmptyUdpResponseReceived(_logger, targetServer);
                    return null;
                }

                if (DnsWireParser.TryParse(buffer.AsSpan(0, result.ReceivedBytes), out DnsWireMessage? message) && message is not null)
                {
                    return message;
                }

                LogUdpWireParseFailed(_logger, targetServer);
                return null;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogUdpQueryException(_logger, targetServer, ex);
            return null;
        }
    }

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Warning,
        Message = "Empty zero-byte UDP payload received from upstream DNS server {TargetServer}")]
    private static partial void LogEmptyUdpResponseReceived(ILogger logger, IPAddress targetServer);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Warning,
        Message = "Failed to parse DNS wire message payload from UDP server {TargetServer}")]
    private static partial void LogUdpWireParseFailed(ILogger logger, IPAddress targetServer);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Error,
        Message = "Exception encountered during UDP DNS query to target server {TargetServer}")]
    private static partial void LogUdpQueryException(ILogger logger, IPAddress targetServer, Exception exception);
}
