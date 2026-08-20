// File: src/Astrolabed.Dns/Upstream/IUpstreamClientFactory.cs
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Upstream;

public interface IUpstreamClientFactory
{
    Task<DnsWireMessage?> ExecuteQueryAsync(string targetServer, byte[] rawRequest, CancellationToken ct);
}
