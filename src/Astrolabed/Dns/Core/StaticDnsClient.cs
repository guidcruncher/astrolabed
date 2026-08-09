using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Core;

public sealed class StaticDnsClient : IDnsClient
{
    private readonly IPAddress _ip;

    public StaticDnsClient(IPAddress ip)
    {
        _ip = ip;
    }

    public Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        try
        {
            // Build a correct single-answer static IP response using the shared builder
            return Task.FromResult(DnsResponseBuilder.BuildStaticIpResponse(request, _ip, ttlSeconds: 60));
        }
        catch
        {
            // If building fails, return a safe SERVFAIL response
            return Task.FromResult(DnsResponseBuilder.BuildServfail(request));
        }
    }
}
