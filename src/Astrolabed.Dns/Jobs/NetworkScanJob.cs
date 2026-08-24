using Astrolabed.Core.Scheduler;
using Astrolabed.Data.Models;
using Astrolabed.Data.Repositories;
using Astrolabed.Dns.Services;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Jobs;

/// <summary>
/// Scheduled background job responsible for periodically scanning the local network segment for active devices
/// and persisting discovery results.
/// </summary>
/// <param name="scanner">LAN device discovery scanner service.</param>
/// <param name="repository">Repository for persisting discovered devices.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class NetworkScanJob(
    INetworkScannerService scanner,
    IDiscoveredLanDeviceRepository repository,
    ILogger<NetworkScanJob> logger) : IScheduledJob
{
    private readonly INetworkScannerService _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    private readonly IDiscoveredLanDeviceRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger<NetworkScanJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public JobSchedule Schedule => JobSchedule.FromInterval(TimeSpan.FromMinutes(15), runOnStartup: true);

    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DiscoveredLanDevice> devices = await _scanner.ScanLanAsync(cancellationToken).ConfigureAwait(false);
        await _repository.BulkUpsertAsync(devices, cancellationToken).ConfigureAwait(false);

        LogJobExecutedSuccessfully(_logger, DateTimeOffset.UtcNow, devices.Count);
    }

    [LoggerMessage(
        EventId = 601,
        Level = LogLevel.Information,
        Message = "Network Scanner Job executed successfully at: {Time} found {Count} devices.")]
    private static partial void LogJobExecutedSuccessfully(ILogger logger, DateTimeOffset time, int count);
}
