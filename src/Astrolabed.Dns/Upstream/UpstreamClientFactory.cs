// File: src/Astrolabed.Dns/Upstream/UpstreamClientFactory.cs
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Models;

using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Dns.Upstream;

public enum TransportProtocol
{
    Udp,
    Tcp,
    Doh
}

public class UpstreamClientFactory : IUpstreamClientFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UpstreamClientFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<DnsWireMessage?> ExecuteQueryAsync(string targetServer, byte[] rawRequest, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(targetServer))
        {
            return null;
        }

        var (ipAddress, protocol) = ParseTargetServer(targetServer);
        if (ipAddress == null)
        {
            return null;
        }

        IDnsUpstreamClient client = protocol switch
        {
            TransportProtocol.Tcp => _serviceProvider.GetRequiredService<TcpUpstreamDnsClient>(),
            TransportProtocol.Doh => _serviceProvider.GetRequiredService<DoHUpstreamDnsClient>(),
            _ => _serviceProvider.GetRequiredService<UdpUpstreamDnsClient>()
        };

        return await client.QueryAsync(ipAddress, rawRequest, ct).ConfigureAwait(false);
    }

    public static (IPAddress? Address, TransportProtocol Protocol) ParseTargetServer(string targetServer)
    {

        if (targetServer.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(targetServer, UriKind.Absolute, out var uri) &&
                IPAddress.TryParse(uri.Host, out var dohIp))
            {
                return (dohIp, TransportProtocol.Doh);
            }

            return (null, TransportProtocol.Doh);
        }

        if (targetServer.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        {
            string host = targetServer["tcp://".Length..].Split(':')[0];
            if (IPAddress.TryParse(host, out var tcpIp))
            {
                return (tcpIp, TransportProtocol.Tcp);
            }

            return (null, TransportProtocol.Tcp);
        }

        if (targetServer.StartsWith("udp://", StringComparison.OrdinalIgnoreCase))
        {
            string host = targetServer["udp://".Length..].Split(':')[0];
            if (IPAddress.TryParse(host, out var udpIp))
            {
                return (udpIp, TransportProtocol.Udp);
            }

            return (null, TransportProtocol.Udp);
        }

        if (IPAddress.TryParse(targetServer, out var rawIp))
        {
            return (rawIp, TransportProtocol.Udp);
        }

        return (null, TransportProtocol.Udp);
    }
}
