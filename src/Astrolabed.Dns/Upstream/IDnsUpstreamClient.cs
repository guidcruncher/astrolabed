// File: src/Astrolabed.Dns/Upstream/IDnsUpstreamClient.cs
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Upstream;

public interface IDnsUpstreamClient
{
    Task<DnsWireMessage?> QueryAsync(IPAddress targetServer, byte[] rawRequest, CancellationToken ct);
}
