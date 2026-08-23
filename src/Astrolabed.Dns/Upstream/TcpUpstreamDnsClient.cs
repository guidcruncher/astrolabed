using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Serialization;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Upstream;

/// <summary>
/// Provides RFC 1035 compliant TCP upstream DNS resolution using high-performance socket streaming.
/// </summary>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class TcpUpstreamDnsClient(
    ILogger<TcpUpstreamDnsClient> logger) : IDnsUpstreamClient
{
    private const int DefaultDnsPort = 53;
    private const int TimeoutMilliseconds = 2000;

    private readonly ILogger<TcpUpstreamDnsClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DnsWireMessage?> QueryAsync(IPAddress targetServer, ReadOnlyMemory<byte> rawRequest, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(targetServer);

        var upstreamEp = new IPEndPoint(targetServer, DefaultDnsPort);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeoutMilliseconds);

        using var socket = new Socket(targetServer.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            ReceiveTimeout = TimeoutMilliseconds,
            SendTimeout = TimeoutMilliseconds
        };

        try
        {
            await socket.ConnectAsync(upstreamEp, cts.Token).ConfigureAwait(false);
            await using var stream = new NetworkStream(socket, ownsSocket: false);

            Span<byte> lengthBuffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(lengthBuffer, (ushort)rawRequest.Length);

            await stream.WriteAsync(lengthBuffer.ToArray(), cts.Token).ConfigureAwait(false);
            await stream.WriteAsync(rawRequest, cts.Token).ConfigureAwait(false);
            await stream.FlushAsync(cts.Token).ConfigureAwait(false);

            byte[] headerBuffer = ArrayPool<byte>.Shared.Rent(2);
            try
            {
                int headerBytesRead = await ReadExactAsync(stream, headerBuffer.AsMemory(0, 2), cts.Token).ConfigureAwait(false);
                if (headerBytesRead < 2)
                {
                    LogTruncatedTcpLengthHeader(_logger, targetServer);
                    return null;
                }

                ushort responseLength = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(0, 2));
                if (responseLength == 0)
                {
                    LogEmptyTcpResponsePayload(_logger, targetServer);
                    return null;
                }

                byte[] responseBuffer = ArrayPool<byte>.Shared.Rent(responseLength);
                try
                {
                    int responseBytesRead = await ReadExactAsync(stream, responseBuffer.AsMemory(0, responseLength), cts.Token).ConfigureAwait(false);
                    if (responseBytesRead < responseLength)
                    {
                        LogTruncatedTcpResponsePayload(_logger, targetServer, responseBytesRead, responseLength);
                        return null;
                    }

                    if (DnsWireParser.TryParse(responseBuffer.AsSpan(0, responseLength), out DnsWireMessage? message) && message is not null)
                    {
                        return message;
                    }

                    LogTcpWireParseFailed(_logger, targetServer);
                    return null;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(responseBuffer);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(headerBuffer);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogTcpQueryException(_logger, targetServer, ex);
            return null;
        }
    }

    private static async Task<int> ReadExactAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], ct).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Warning,
        Message = "Truncated 2-byte TCP length header received from upstream server {TargetServer}")]
    private static partial void LogTruncatedTcpLengthHeader(ILogger logger, IPAddress targetServer);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Warning,
        Message = "Empty zero-length TCP DNS payload received from upstream server {TargetServer}")]
    private static partial void LogEmptyTcpResponsePayload(ILogger logger, IPAddress targetServer);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Warning,
        Message = "Truncated TCP DNS response payload received from upstream server {TargetServer}. Read {BytesRead}/{ExpectedBytes} bytes.")]
    private static partial void LogTruncatedTcpResponsePayload(ILogger logger, IPAddress targetServer, int bytesRead, int expectedBytes);

    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Warning,
        Message = "Failed to parse DNS wire message payload from TCP server {TargetServer}")]
    private static partial void LogTcpWireParseFailed(ILogger logger, IPAddress targetServer);

    [LoggerMessage(
        EventId = 105,
        Level = LogLevel.Error,
        Message = "Exception encountered during TCP DNS query to target server {TargetServer}")]
    private static partial void LogTcpQueryException(ILogger logger, IPAddress targetServer, Exception exception);
}
