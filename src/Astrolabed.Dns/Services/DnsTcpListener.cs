using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Asynchronous TCP listener for handling inbound stream-based DNS queries with 2-byte prefix framing.
/// </summary>
/// <param name="queryProcessor">DNS query processing engine.</param>
/// <param name="optionsMonitor">Monitored DNS engine options.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class DnsTcpListener(
    IDnsQueryProcessor queryProcessor,
    IOptionsMonitor<DnsEngineOptions> optionsMonitor,
    ILogger<DnsTcpListener> logger) : IDnsListener
{
    private readonly IDnsQueryProcessor _queryProcessor = queryProcessor ?? throw new ArgumentNullException(nameof(queryProcessor));
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly ILogger<DnsTcpListener> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task ListenAsync(IPAddress address, int port, CancellationToken ct)
    {
        var listener = new TcpListener(address, port);
        LogStartingTcpListener(_logger, address, port);

        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();

        LogTcpListenerStarted(_logger, address, port);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient tcpClient = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                _ = ProcessConnectionSafelyAsync(tcpClient, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Graceful shutdown requested
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task ProcessConnectionSafelyAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            await HandleTcpConnectionAsync(client, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Graceful shutdown requested
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            LogConnectionClosedByPeer(_logger, ex);
        }
        catch (Exception ex)
        {
            LogUnexpectedConnectionError(_logger, ex);
        }
    }

    private async Task HandleTcpConnectionAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        await using (NetworkStream stream = client.GetStream())
        {
            EndPoint remoteEndPoint = client.Client?.RemoteEndPoint ?? new IPEndPoint(IPAddress.Loopback, 0);
            byte[] lengthBuffer = new byte[2];

            while (!ct.IsCancellationRequested && client.Connected)
            {
                if (!await ReadExactAsync(stream, lengthBuffer, ct).ConfigureAwait(false))
                {
                    break;
                }

                ushort packetLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer);
                if (packetLength == 0)
                {
                    continue;
                }

                byte[] packetBuffer = ArrayPool<byte>.Shared.Rent(packetLength);
                try
                {
                    Memory<byte> memoryBuffer = packetBuffer.AsMemory(0, packetLength);
                    if (!await ReadExactAsync(stream, memoryBuffer, ct).ConfigureAwait(false))
                    {
                        break;
                    }

                    // Process payload without creating array copies
                    byte[]? response = await _queryProcessor
                        .ProcessRequestAsync(packetBuffer[..packetLength], remoteEndPoint, ct)
                        .ConfigureAwait(false);

                    if (response is { Length: > 0 })
                    {
                        int sendLength = 2 + response.Length;
                        byte[] writeBuffer = ArrayPool<byte>.Shared.Rent(sendLength);
                        try
                        {
                            BinaryPrimitives.WriteUInt16BigEndian(writeBuffer, (ushort)response.Length);
                            Buffer.BlockCopy(response, 0, writeBuffer, 2, response.Length);

                            await stream.WriteAsync(writeBuffer.AsMemory(0, sendLength), ct).ConfigureAwait(false);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(writeBuffer);
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(packetBuffer);
                }
            }
        }
    }

    private static async Task<bool> ReadExactAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], ct).ConfigureAwait(false);
            if (read == 0)
            {
                return false;
            }
            totalRead += read;
        }
        return true;
    }

    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Information,
        Message = "Starting TCP Listener on {Address}#{Port}")]
    private static partial void LogStartingTcpListener(ILogger logger, IPAddress address, int port);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Information,
        Message = "TCP Listener Started on {Address}#{Port}")]
    private static partial void LogTcpListenerStarted(ILogger logger, IPAddress address, int port);

    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Debug,
        Message = "TCP connection closed by remote peer.")]
    private static partial void LogConnectionClosedByPeer(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 204,
        Level = LogLevel.Error,
        Message = "Unexpected error handling TCP connection.")]
    private static partial void LogUnexpectedConnectionError(ILogger logger, Exception exception);
}
