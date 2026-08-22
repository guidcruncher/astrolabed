using Astrolabed.Data.Models;

namespace Astrolabed.Dns.Services;

public class NetworkScannerOptions
{
    public const string SectionName = "NetworkScanner";

    public int MaxDegreeOfParallelism { get; set; } = 100;
    public int PingTimeoutMs { get; set; } = 200;
}

public interface INetworkScannerService
{
    Task<IReadOnlyCollection<DiscoveredLanDevice>> ScanLanAsync(CancellationToken cancellationToken = default);
}
