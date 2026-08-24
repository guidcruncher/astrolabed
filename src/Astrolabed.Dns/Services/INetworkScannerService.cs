// File: src/Astrolabed.Dns/Services/INetworkScannerService.cs
using Astrolabed.Data.Models;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Service contract for discovering active hardware devices and network interfaces on the local network segment.
/// </summary>
public interface INetworkScannerService
{
    /// <summary>
    /// Asynchronously scans the local area network (LAN) to detect active connected devices.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the network scan operation. The task result contains a collection 
    /// of <see cref="DiscoveredLanDevice"/> instances representing discovered active devices.
    /// </returns>
    Task<IReadOnlyCollection<DiscoveredLanDevice>> ScanLanAsync(CancellationToken cancellationToken = default);
}
