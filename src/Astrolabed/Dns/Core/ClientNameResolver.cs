using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Astrolabed;
using Astrolabed.Dhcp;
using Astrolabed.Dns.ConditionalForwarding;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Core;

public sealed class ClientNameResolver : IClientNameResolver
{

    private readonly ILogger<ClientNameResolver> _logger;
    private readonly IConditionalDnsForwarder _conditionalForwarder;
    private readonly IDhcpLeaseReader _dhcpReader;

    public ClientNameResolver(
        ILogger<ClientNameResolver> logger,
        IConditionalDnsForwarder conditionalForwarder,
        IDhcpLeaseReader dhcpReader)
    {
        _logger = logger;
        _conditionalForwarder = conditionalForwarder;
        _dhcpReader = dhcpReader;
    }

    public async Task<string?> Resolve(IPAddress clientIp, CancellationToken ct)
    {
        try
        {
            if (_dhcpReader.Enabled())
            {
                var lease = await _dhcpReader.GetLeaseByIpAsync(clientIp, ct);
                if (lease != null)
                {
                    if (!string.IsNullOrEmpty(lease.ClientName)) { return lease.ClientName; }
                }

            }

            string ptrQueryName = FormatPtrDomain(clientIp);

            const ushort ptrQueryType = 12; // PTR record type

            if (ConditionalDnsForwarder.IsLocalhost(ptrQueryName))
            {
                return "localhost";
            }

            if (!_conditionalForwarder.ShouldForwardToLocalDhcp(ptrQueryName, ptrQueryType))
            {
                return null;
            }

            byte[] ptrRequestPacket = DnsMessage.CreatePtrQuery(ptrQueryName);
            byte[] responseBuffer = await _conditionalForwarder.ForwardToLocalDhcpAsync(ptrRequestPacket, ct).ConfigureAwait(false);

            if (responseBuffer.Length == 0)
            {
                return null;
            }

            var parsedResponse = DnsMessage.TryParse(responseBuffer);
            return parsedResponse?.AnswerHostName;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to resolve PTR client hostname for {ClientIp}", clientIp);
            return null;
        }
    }

    private static string FormatPtrDomain(IPAddress ip)
    {
        byte[] bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return $"{bytes[3]}.{bytes[2]}.{bytes[1]}.{bytes[0]}.in-addr.arpa";
        }

        var nibbles = new char[64];
        for (int i = 0; i < 16; i++)
        {
            byte b = bytes[15 - i];
            nibbles[i * 4] = GetHexChar(b & 0x0F);
            nibbles[i * 4 + 1] = '.';
            nibbles[i * 4 + 2] = GetHexChar((b >> 4) & 0x0F);
            nibbles[i * 4 + 3] = '.';
        }

        return new string(nibbles) + "ip6.arpa";
    }

    private static char GetHexChar(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));

}
