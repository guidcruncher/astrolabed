using System.Net;

namespace Astrolabed.Dns.Models;

public sealed class DnsContext
{

    public Guid Id { get; private set; }

    public IPAddress ClientIp { get; private set; }

    public DnsContext(IPAddress clientIp)
    {
        Id = Guid.CreateVersion7();
        ClientIp = clientIp;
    }

}
