// File: src/Astrolabed.Data/Jobs/CleanUpDnsActivityJob.cs
using Astrolabed.Core.Scheduler;
using Astrolabed.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Data.Jobs;

/// <summary>
/// Scheduled background job responsible for periodically purging aged DNS response event telemetry.
/// </summary>
/// <param name="repository">Repository handling DNS event record persistence.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class CleanUpDnsActivityJob(
    IDnsResponseEventRepository repository,
    ILogger<CleanUpDnsActivityJob> logger) : IScheduledJob
{
    private readonly IDnsResponseEventRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger<CleanUpDnsActivityJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public JobSchedule Schedule => JobSchedule.EverySunday(new TimeSpan(3, 0, 0), runOnStartup: true);

    /// <inheritdoc />
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _repository.CleanOldDataAsync(cancellationToken).ConfigureAwait(false);
        LogJobExecutedSuccessfully(_logger, DateTimeOffset.UtcNow);
    }

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Information,
        Message = "Clean Up Dns Activity Job executed successfully at: {Time}")]
    private static partial void LogJobExecutedSuccessfully(ILogger logger, DateTimeOffset time);
}
