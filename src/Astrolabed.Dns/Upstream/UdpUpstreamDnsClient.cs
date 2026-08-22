// File: src/Astrolabed.Dns/Upstream/UdpUpstreamDnsClient.cs
using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Serialization;

namespace Astrolabed.Dns.Upstream;

public class UdpUpstreamDnsClient : IDnsUpstreamClient
{
    public async Task<DnsWireMessage?> QueryAsync(IPAddress targetServer, byte[] rawRequest, CancellationToken ct)
    {
        var upstreamEp = new IPEndPoint(targetServer, 53);
        using var upstreamSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        upstreamSocket.ReceiveTimeout = 2000;

        try
        {
            await upstreamSocket.SendToAsync(rawRequest, SocketFlags.None, upstreamEp, ct).ConfigureAwait(false);
            var buffer = new byte[4096];
            var result = await upstreamSocket.ReceiveFromAsync(buffer, SocketFlags.None, upstreamEp, ct).ConfigureAwait(false);

            var responseBytes = buffer.AsSpan(0, result.ReceivedBytes).ToArray();

            if (DnsWireParser.TryParse(responseBytes, out var message) && message != null)
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
}
