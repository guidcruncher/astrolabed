using Astrolabed.Core.Scheduler;
using Astrolabed.Data.Repositories;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Data.Jobs;

public class CleanUpDnsActivityJob : IScheduledJob
{
    private readonly ILogger<CleanUpDnsActivityJob> _logger;
    private readonly IDnsResponseEventRepository _repository;

    public JobSchedule Schedule => JobSchedule.EverySunday(new TimeSpan(3, 0, 0), runOnStartup: true);

    public CleanUpDnsActivityJob(
        IDnsResponseEventRepository repository,
        ILogger<CleanUpDnsActivityJob> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _repository.CleanOldData(cancellationToken);
        _logger.LogInformation("Clean Up Dns Activity Job executed successfully at: {Time}", DateTimeOffset.UtcNow);
    }
}
