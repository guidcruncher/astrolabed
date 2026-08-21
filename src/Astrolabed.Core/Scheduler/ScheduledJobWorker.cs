using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Core.Scheduler;

public class ScheduledJobWorker<TJob> : BackgroundService where TJob : IScheduledJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledJobWorker<TJob>> _logger;
    private readonly TimeProvider _timeProvider;

    public ScheduledJobWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledJobWorker<TJob>> logger,
        TimeProvider? timeProvider = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled background worker for {JobType} started.", typeof(TJob).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            JobSchedule schedule;
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                TJob job = scope.ServiceProvider.GetRequiredService<TJob>();
                schedule = job.Schedule;
            }

            TimeSpan delay = CalculateNextDelay(schedule);

            _logger.LogInformation(
                "Next execution for {JobType} scheduled in {TotalSeconds} seconds.",
                typeof(TJob).Name,
                delay.TotalSeconds);

            try
            {
                await Task.Delay(delay, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker execution for {JobType} was canceled.", typeof(TJob).Name);
                break;
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunJobScopeAsync(stoppingToken);
        }
    }

    private TimeSpan CalculateNextDelay(JobSchedule schedule)
    {
        DateTimeOffset now = _timeProvider.GetLocalNow();

        if (schedule.PreferredTimeOfDay.HasValue)
        {
            DateTimeOffset targetToday = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset)
                .Add(schedule.PreferredTimeOfDay.Value);

            DateTimeOffset nextRun = now >= targetToday
                ? targetToday.Add(schedule.Interval)
                : targetToday;

            return nextRun - now;
        }

        return schedule.Interval;
    }

    private async Task RunJobScopeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled execution for {JobType}.", typeof(TJob).Name);

        try
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                TJob job = scope.ServiceProvider.GetRequiredService<TJob>();
                await job.ExecuteAsync(cancellationToken);
            }

            _logger.LogInformation("Finished execution for {JobType}.", typeof(TJob).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while executing job {JobType}.", typeof(TJob).Name);
        }
    }
}
