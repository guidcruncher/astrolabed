using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Core;

public sealed class UdpDnsClient : IDnsClient
{
    private readonly IPEndPoint _endpoint;

    public UdpDnsClient(IPEndPoint endpoint)
    {
        _endpoint = endpoint;
    }

    public async Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        using var udp = new UdpClient();
        udp.Connect(_endpoint);

        await udp.SendAsync(request, request.Length);

        var result = await udp.ReceiveAsync(ct);
        var resp = result.Buffer;

        try
        {
            if (resp == null || resp.Length < 12)
                throw new System.ArgumentException("Upstream response too short");

            return resp;
        }
        catch
        {
            // Return a safe SERVFAIL if upstream reply is malformed
            return DnsResponseBuilder.BuildServfail(request);
        }
    }
}
