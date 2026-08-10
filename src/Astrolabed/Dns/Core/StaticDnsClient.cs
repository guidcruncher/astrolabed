using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Core;

public sealed class StaticDnsClient : IDnsClient
{
    private readonly IPAddress _ip;

    public StaticDnsClient(IPAddress ip)
    {
        ArgumentNullException.ThrowIfNull(ip);
        _ip = ip;
    }

    public Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            byte[] response = DnsResponseBuilder.BuildStaticIpResponse(request, _ip, ttlSeconds: 60);
            return Task.FromResult(response);
        }
        catch
        {
            byte[] servfail = DnsResponseBuilder.BuildServfail(request);
            return Task.FromResult(servfail);
        }
    }
}
