using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.ConditionalForwarding;

public class ConditionalDnsForwarder : IConditionalDnsForwarder
{
    private readonly IOptionsMonitor<DnsForwarderOptions> _options;
    private readonly ILogger<ConditionalDnsForwarder> _logger;

    public ConditionalDnsForwarder(
        IOptionsMonitor<DnsForwarderOptions> options,
        ILogger<ConditionalDnsForwarder> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool ShouldForwardToLocalDhcp(string queryName, ushort queryType)
    {
        var opts = _options.CurrentValue.ConditionalForwarding;
        if (!opts.Enabled)
        {
            return false;
        }

        var normalizedName = queryName.TrimEnd('.').ToLowerInvariant();

        // 1. Check for PTR reverse lookup requests matching local IP ranges (.in-addr.arpa)
        if (queryType == 12 && normalizedName.EndsWith("in-addr.arpa", StringComparison.OrdinalIgnoreCase))
        {
            return IsLocalReverseLookup(normalizedName);
        }

        // 2. Check for configured local domain suffix (e.g. myhost.lan -> .lan)
        if (!string.IsNullOrWhiteSpace(opts.LocalDomain) &&
            normalizedName.EndsWith(opts.LocalDomain.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 3. Check for non-FQDN requests (single-label hostnames without dots like 'nas' or 'printer')
        if (opts.ForwardNonFqdn && !normalizedName.Contains('.'))
        {
            return true;
        }

        return false;
    }

    public async Task<byte[]> ForwardToLocalDhcpAsync(byte[] dnsQueryBuffer, CancellationToken cancellationToken = default)
    {
        var opts = _options.CurrentValue.ConditionalForwarding;
        var endPoint = new IPEndPoint(IPAddress.Parse(opts.DhcpServerIp), opts.DhcpServerPort);

        using var client = new UdpClient();
        client.Client.SendTimeout = 2000;
        client.Client.ReceiveTimeout = 2000;

        _logger.LogDebug("Forwarding DNS request to local DHCP server/router at {EndPoint}", endPoint);

        await client.SendAsync(dnsQueryBuffer, dnsQueryBuffer.Length, endPoint);
        var result = await client.ReceiveAsync(cancellationToken);

        return result.Buffer;
    }

    private bool IsLocalReverseLookup(string ptrName)
    {
        // Simple heuristic: matches reverse queries like 50.1.168.192.in-addr.arpa
        var opts = _options.CurrentValue.ConditionalForwarding;
        var subnetPrefix = string.Join(".", opts.LocalSubnetCidr.Split('/')[0].Split('.').Take(3).Reverse());

        return ptrName.Contains(subnetPrefix, StringComparison.OrdinalIgnoreCase);
    }
}

