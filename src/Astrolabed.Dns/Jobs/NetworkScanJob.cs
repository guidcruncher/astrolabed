using Astrolabed.Core.Scheduler;
using Astrolabed.Data.Repositories;
using Astrolabed.Dns.Services;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Jobs;

public class NetworkScanJob : IScheduledJob
{
    private readonly ILogger<NetworkScanJob> _logger;
    private readonly INetworkScannerService _scanner;
    private readonly IDiscoveredLanDeviceRepository _repository;

    public JobSchedule Schedule => JobSchedule.EverySunday(new TimeSpan(3, 0, 0));

    public NetworkScanJob(
    INetworkScannerService scanner,
    IDiscoveredLanDeviceRepository repository,
        ILogger<NetworkScanJob> logger)
    {
        _logger = logger;
        _repository = repository;
        _scanner = scanner;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var devices = await _scanner.ScanLanAsync(cancellationToken);
        await _repository.BulkUpsertAsync(devices, cancellationToken);
        _logger.LogInformation("Network Scanner Job executed successfully at: {Time} found {Count} devices.", DateTimeOffset.UtcNow, devices.Count);
    }
}
