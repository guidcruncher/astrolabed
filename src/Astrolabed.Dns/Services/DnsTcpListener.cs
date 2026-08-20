// File: src/Astrolabed.Dns/Services/DnsTcpListener.cs
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;


namespace Astrolabed.Dns.Services;

public sealed class DnsTcpListener : IDnsListener
{
    private readonly IDnsQueryProcessor _queryProcessor;
    private readonly ILogger<DnsTcpListener> _logger;

    public DnsTcpListener(IDnsQueryProcessor queryProcessor, ILogger<DnsTcpListener> logger)
    {
        _queryProcessor = queryProcessor;
        _logger = logger;
    }

    public async Task ListenAsync(IPAddress address, int port, CancellationToken ct)
    {
        var listener = new TcpListener(address, port);
        _logger.LogInformation("Starting Tcp Listener on {Address}#{Port}", address.ToString(), port.ToString());
        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();
        _logger.LogInformation("Tcp Listener Started on {Address}#{Port}", address.ToString(), port.ToString());

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                _ = Task.Run(() => HandleTcpConnectionAsync(tcpClient, ct), ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleTcpConnectionAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            var lengthBuffer = new byte[2];
            var remoteEndPoint = client.Client?.RemoteEndPoint ?? new IPEndPoint(IPAddress.Loopback, 0);

            while (!ct.IsCancellationRequested && client.Connected)
            {
                int bytesRead = await ReadExactAsync(stream, lengthBuffer, 0, 2, ct).ConfigureAwait(false);
                if (bytesRead < 2) break;

                ushort packetLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer);
                if (packetLength == 0) continue;

                var packetBuffer = ArrayPool<byte>.Shared.Rent(packetLength);
                try
                {
                    int packetBytesRead = await ReadExactAsync(stream, packetBuffer, 0, packetLength, ct).ConfigureAwait(false);
                    if (packetBytesRead < packetLength) break;

                    byte[]? response = await _queryProcessor.ProcessRequestAsync(packetBuffer.AsSpan(0, packetLength).ToArray(), remoteEndPoint, ct).ConfigureAwait(false);

                    if (response != null)
                    {
                        var responseLengthBuffer = new byte[2];
                        BinaryPrimitives.WriteUInt16BigEndian(responseLengthBuffer, (ushort)response.Length);

                        await stream.WriteAsync(responseLengthBuffer, ct).ConfigureAwait(false);
                        await stream.WriteAsync(response, ct).ConfigureAwait(false);
                        await stream.FlushAsync(ct).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(packetBuffer);
                }
            }
        }
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), ct).ConfigureAwait(false);
            if (read == 0) break;
            totalRead += read;
        }
        return totalRead;
    }
}
