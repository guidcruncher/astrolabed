namespace Astrolabed.Core.Scheduler;

/// <summary>
/// Defines immutable timing rules, interval constraints, and time-of-day preferences 
/// for background job execution.
/// </summary>
public sealed record JobSchedule
{
    /// <summary>
    /// Gets the periodic execution interval between job runs.
    /// </summary>
    public TimeSpan Interval { get; }

    /// <summary>
    /// Gets the optional target time of day for job execution.
    /// </summary>
    public TimeSpan? PreferredTimeOfDay { get; }

    /// <summary>
    /// Gets the optional target day of the week for weekly job execution schedules.
    /// </summary>
    public DayOfWeek? TargetDayOfWeek { get; }

    /// <summary>
    /// Gets a value indicating whether the job should execute immediately when the worker service initializes.
    /// </summary>
    public bool RunOnStartup { get; }

    private JobSchedule(
        TimeSpan interval,
        TimeSpan? preferredTimeOfDay = null,
        DayOfWeek? targetDayOfWeek = null,
        bool runOnStartup = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);

        if (preferredTimeOfDay.HasValue && (preferredTimeOfDay.Value < TimeSpan.Zero || preferredTimeOfDay.Value >= TimeSpan.FromDays(1)))
        {
            throw new ArgumentOutOfRangeException(
                nameof(preferredTimeOfDay),
                preferredTimeOfDay,
                "Preferred time of day must be non-negative and less than 24 hours.");
        }

        Interval = interval;
        PreferredTimeOfDay = preferredTimeOfDay;
        TargetDayOfWeek = targetDayOfWeek;
        RunOnStartup = runOnStartup;
    }

    /// <summary>
    /// Creates a job schedule based solely on a repeating execution time interval.
    /// </summary>
    /// <param name="interval">The duration to wait between executions.</param>
    /// <param name="runOnStartup">Whether to run the job immediately at worker service startup.</param>
    /// <returns>A configured <see cref="JobSchedule"/> instance.</returns>
    public static JobSchedule FromInterval(TimeSpan interval, bool runOnStartup = false) =>
        new(interval, runOnStartup: runOnStartup);

    /// <summary>
    /// Creates a job schedule that executes once daily at a specified target time of day.
    /// </summary>
    /// <param name="timeOfDay">The target time of day (e.g. 02:00:00 for 2 AM).</param>
    /// <param name="runOnStartup">Whether to run the job immediately at worker service startup.</param>
    /// <returns>A configured <see cref="JobSchedule"/> instance.</returns>
    public static JobSchedule DailyAt(TimeSpan timeOfDay, bool runOnStartup = false) =>
        new(TimeSpan.FromDays(1), timeOfDay, runOnStartup: runOnStartup);

    /// <summary>
    /// Creates a job schedule that executes every minute.
    /// </summary>
    /// <param name="runOnStartup">Whether to run the job immediately at worker service startup.</param>
    /// <returns>A configured <see cref="JobSchedule"/> instance.</returns>
    public static JobSchedule EveryMinute(bool runOnStartup = false) =>
        new(TimeSpan.FromMinutes(1), runOnStartup: runOnStartup);

    /// <summary>
    /// Creates a job schedule that executes once per week on a specified target day and optional time of day.
    /// </summary>
    /// <param name="dayOfWeek">The target day of the week.</param>
    /// <param name="timeOfDay">An optional target time of day on that day.</param>
    /// <param name="runOnStartup">Whether to run the job immediately at worker service startup.</param>
    /// <returns>A configured <see cref="JobSchedule"/> instance.</returns>
    public static JobSchedule WeeklyOn(DayOfWeek dayOfWeek, TimeSpan? timeOfDay = null, bool runOnStartup = false) =>
        new(TimeSpan.FromDays(7), timeOfDay ?? TimeSpan.Zero, dayOfWeek, runOnStartup: runOnStartup);

    /// <summary>
    /// Creates a job schedule that executes every Sunday.
    /// </summary>
    /// <param name="timeOfDay">An optional target time of day on Sunday.</param>
    /// <param name="runOnStartup">Whether to run the job immediately at worker service startup.</param>
    /// <returns>A configured <see cref="JobSchedule"/> instance.</returns>
    public static JobSchedule EverySunday(TimeSpan? timeOfDay = null, bool runOnStartup = false) =>
        WeeklyOn(DayOfWeek.Sunday, timeOfDay, runOnStartup: runOnStartup);
}
