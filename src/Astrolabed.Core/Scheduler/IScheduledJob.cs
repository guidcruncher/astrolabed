namespace Astrolabed.Core.Scheduler;

/// <summary>
/// Defines the contract for background scheduled maintenance and background tasks
/// managed by the application job scheduler.
/// </summary>
public interface IScheduledJob
{
    /// <summary>
    /// Gets the operational execution schedule configuration for the background job.
    /// </summary>
    JobSchedule Schedule { get; }

    /// <summary>
    /// Asynchronously executes the scheduled job operation.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during job execution.</param>
    /// <returns>A task representing the asynchronous background execution process.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

