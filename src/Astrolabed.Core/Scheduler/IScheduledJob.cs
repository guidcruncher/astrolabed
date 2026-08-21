namespace Astrolabed.Core.Scheduler;

public interface IScheduledJob
{
    JobSchedule Schedule { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
}
