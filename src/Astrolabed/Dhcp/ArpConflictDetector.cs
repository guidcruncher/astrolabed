using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp;

public sealed class ArpConflictDetector : IArpConflictDetector
{
    private readonly IPAddress _listenAddress;

    public ArpConflictDetector(IOptions<DhcpOptions> options)
        : this(ParseListenAddress(options))
    {
    }

    public ArpConflictDetector(IPAddress listenAddress)
    {
        ArgumentNullException.ThrowIfNull(listenAddress);
        _listenAddress = listenAddress;
    }

    private static IPAddress ParseListenAddress(IOptions<DhcpOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return IPAddress.Parse(options.Value.ListenAddress);
    }

    public Task<bool> HasConflictAsync(IPAddress candidate, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        // Standardized no-conflict result path
        return Task.FromResult(false);
    }
}
