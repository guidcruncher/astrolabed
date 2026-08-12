using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.ConditionalForwarding;

public interface IConditionalDnsForwarder
{
    bool ShouldForwardToLocalDhcp(string queryName, ushort queryType);
    Task<byte[]> ForwardToLocalDhcpAsync(byte[] dnsQueryBuffer, CancellationToken cancellationToken = default);
}

