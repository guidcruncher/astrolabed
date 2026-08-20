// File: src/Astrolabed.Dns/Services/DnsTcpListener.cs
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

public sealed class DnsTcpListener : IDnsListener
{
    private readonly IDnsQueryProcessor _queryProcessor;
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;
    private readonly ILogger<DnsTcpListener> _logger;

    public DnsTcpListener(
        IDnsQueryProcessor queryProcessor,
        IOptionsMonitor<DnsEngineOptions> optionsMonitor,
        ILogger<DnsTcpListener> logger)
    {
        _queryProcessor = queryProcessor;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task ListenAsync(IPAddress address, int port, CancellationToken ct)
    {
        var listener = new TcpListener(address, port);
        _logger.LogInformation("Starting TCP Listener on {Address}#{Port}", address.ToString(), port.ToString());

        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();

        _logger.LogInformation("TCP Listener Started on {Address}#{Port}", address.ToString(), port.ToString());

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                _ = ProcessConnectionSafelyAsync(tcpClient, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Expected shutdown signal
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
            // Graceful shutdown
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            _logger.LogDebug(ex, "TCP connection closed by remote peer.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error handling TCP connection.");
        }
    }

    private async Task HandleTcpConnectionAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            var remoteEndPoint = client.Client?.RemoteEndPoint ?? new IPEndPoint(IPAddress.Loopback, 0);
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

                    byte[]? response = await _queryProcessor.ProcessRequestAsync(memoryBuffer.ToArray(), remoteEndPoint, ct).ConfigureAwait(false);

                    if (response != null && response.Length > 0)
                    {
                        int sendLength = 2 + response.Length;
                        byte[] writeBuffer = ArrayPool<byte>.Shared.Rent(sendLength);
                        try
                        {
                            BinaryPrimitives.WriteUInt16BigEndian(writeBuffer, (ushort)response.Length);
                            Buffer.BlockCopy(response, 0, writeBuffer, 2, response.Length);

                            await stream.WriteAsync(writeBuffer.AsMemory(0, sendLength), ct).ConfigureAwait(false);
                            await stream.FlushAsync(ct).ConfigureAwait(false);
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
}
