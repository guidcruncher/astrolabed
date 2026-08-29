namespace Astrolabed.Core.Network;

using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines the service contract for actively probing target hosts to collect diagnostic network signatures.
/// </summary>
public interface INetworkDeviceProbeService
{
    /// <summary>
    /// Actively probes a target IP address to extract TTL, open TCP ports, SSDP headers, and mDNS metadata.
    /// </summary>
    /// <param name="ipAddress">The IP address of the target host.</param>
    /// <param name="macAddress">The pre-resolved physical MAC address of the target host.</param>
    /// <param name="hostname">An optional pre-resolved host name.</param>
    /// <param name="dhcpVendorClass">An optional pre-captured DHCP Option 60 vendor string.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A populated <see cref="NetworkDeviceProbeResult"/> containing all collected telemetry.</returns>
    Task<NetworkDeviceProbeResult> ProbeDeviceAsync(
        IPAddress ipAddress,
        PhysicalAddress macAddress,
        string? hostname = null,
        string? dhcpVendorClass = null,
        CancellationToken cancellationToken = default);
}

