// File: src/Astrolabed.Dns/Upstream/TcpUpstreamDnsClient.cs
using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Serialization;

namespace Astrolabed.Dns.Upstream;

public class TcpUpstreamDnsClient : IDnsUpstreamClient
{
    public async Task<DnsWireMessage?> QueryAsync(IPAddress targetServer, byte[] rawRequest, CancellationToken ct)
    {
        var upstreamEp = new IPEndPoint(targetServer, 53);
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            ReceiveTimeout = 2000,
            SendTimeout = 2000
        };

        try
        {
            await socket.ConnectAsync(upstreamEp, ct).ConfigureAwait(false);
            await using var stream = new NetworkStream(socket, ownsSocket: false);

            var lengthBuffer = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(lengthBuffer, (ushort)rawRequest.Length);

            await stream.WriteAsync(lengthBuffer, ct).ConfigureAwait(false);
            await stream.WriteAsync(rawRequest, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);

            int bytesRead = await ReadExactAsync(stream, lengthBuffer, 0, 2, ct).ConfigureAwait(false);
            if (bytesRead < 2) return null;

            ushort responseLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer);
            if (responseLength == 0) return null;

            var responseBuffer = new byte[responseLength];
            int responseBytesRead = await ReadExactAsync(stream, responseBuffer, 0, responseLength, ct).ConfigureAwait(false);
            if (responseBytesRead < responseLength) return null;

            if (DnsWireParser.TryParse(responseBuffer, out var message) && message != null)
            {
                return message;
            }

            return null;
        }
        catch
        {
            return null;
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

