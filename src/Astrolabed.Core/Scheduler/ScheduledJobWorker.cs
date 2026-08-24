using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Core.Scheduler;

/// <summary>
/// Long-running <see cref="BackgroundService"/> responsible for scheduling and executing <typeparamref name="TJob"/> tasks
/// based on configured time intervals and time-of-day preferences.
/// </summary>
/// <typeparam name="TJob">The scheduled job type implementing <see cref="IScheduledJob"/>.</typeparam>
/// <param name="scopeFactory">The service scope factory for resolving scoped job dependencies.</param>
/// <param name="logger">Structured logger instance for job execution telemetry.</param>
/// <param name="timeProvider">Optional time abstraction for deterministic testing and clock operations.</param>
public sealed partial class ScheduledJobWorker<TJob>(
    IServiceScopeFactory scopeFactory,
    ILogger<ScheduledJobWorker<TJob>> logger,
    TimeProvider? timeProvider = null) : BackgroundService where TJob : IScheduledJob
{
    private static readonly string JobName = typeof(TJob).Name;

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<ScheduledJobWorker<TJob>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(_logger, JobName);

        bool isFirstRun = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            JobSchedule schedule = FetchJobSchedule();

            if (isFirstRun && schedule.RunOnStartup)
            {
                isFirstRun = false;
                await RunJobScopeAsync(stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
            else
            {
                isFirstRun = false;
            }

            TimeSpan delay = CalculateNextDelay(schedule);

            LogNextExecutionScheduled(_logger, JobName, delay.TotalSeconds);

            try
            {
                await Task.Delay(delay, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                LogWorkerCanceled(_logger, JobName);
                break;
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunJobScopeAsync(stoppingToken);
        }
    }

    private JobSchedule FetchJobSchedule()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        TJob job = scope.ServiceProvider.GetRequiredService<TJob>();
        return job.Schedule;
    }

    private TimeSpan CalculateNextDelay(JobSchedule schedule)
    {
        DateTimeOffset now = _timeProvider.GetLocalNow();

        if (schedule.PreferredTimeOfDay.HasValue)
        {
            DateTime localDate = now.LocalDateTime.Date;
            TimeSpan timeOfDay = schedule.PreferredTimeOfDay.Value;

            DateTimeOffset targetToday = new DateTimeOffset(localDate.Add(timeOfDay), now.Offset);

            DateTimeOffset nextRun = now >= targetToday
                ? targetToday.Add(schedule.Interval)
                : targetToday;

            TimeSpan delay = nextRun - now;
            return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }

        return schedule.Interval < TimeSpan.Zero ? TimeSpan.Zero : schedule.Interval;
    }

    private async Task RunJobScopeAsync(CancellationToken cancellationToken)
    {
        LogStartingJobExecution(_logger, JobName);

        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            TJob job = scope.ServiceProvider.GetRequiredService<TJob>();
            await job.ExecuteAsync(cancellationToken);

            LogFinishedJobExecution(_logger, JobName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogWorkerCanceled(_logger, JobName);
        }
        catch (Exception ex)
        {
            LogJobExecutionError(_logger, ex, JobName);
        }
    }

    [LoggerMessage(EventId = 301, Level = LogLevel.Information, Message = "Scheduled background worker for {JobName} started.")]
    private static partial void LogWorkerStarted(ILogger logger, string jobName);

    [LoggerMessage(EventId = 302, Level = LogLevel.Information, Message = "Next execution for {JobName} scheduled in {TotalSeconds} seconds.")]
    private static partial void LogNextExecutionScheduled(ILogger logger, string jobName, double totalSeconds);

    [LoggerMessage(EventId = 303, Level = LogLevel.Information, Message = "Worker execution for {JobName} was canceled.")]
    private static partial void LogWorkerCanceled(ILogger logger, string jobName);

    [LoggerMessage(EventId = 304, Level = LogLevel.Information, Message = "Starting scheduled execution for {JobName}.")]
    private static partial void LogStartingJobExecution(ILogger logger, string jobName);

    [LoggerMessage(EventId = 305, Level = LogLevel.Information, Message = "Finished execution for {JobName}.")]
    private static partial void LogFinishedJobExecution(ILogger logger, string jobName);

    [LoggerMessage(EventId = 306, Level = LogLevel.Error, Message = "An error occurred while executing job {JobName}.")]
    private static partial void LogJobExecutionError(ILogger logger, Exception exception, string jobName);
}
