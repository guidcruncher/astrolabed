using System.Net;

using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp;

public sealed class ArpConflictDetector : IArpConflictDetector
{
    private readonly IPAddress _listenAddress;

    public ArpConflictDetector(IOptions<DhcpOptions> options)
        : this(IPAddress.Parse(options.Value.ListenAddress))
    {
    }

    public ArpConflictDetector(IPAddress listenAddress)
    {
        _listenAddress = listenAddress;
    }

    public Task<bool> HasConflictAsync(IPAddress candidate, TimeSpan timeout)
    {
        return Task.FromResult(false);
    }
}
