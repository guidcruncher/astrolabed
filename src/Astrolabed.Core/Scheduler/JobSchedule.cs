// JobSchedule.cs
namespace Astrolabed.Core.Scheduler;

public class JobSchedule
{
    public TimeSpan Interval { get; }
    public TimeSpan? PreferredTimeOfDay { get; }
    public DayOfWeek? TargetDayOfWeek { get; }
    public bool RunOnStartup { get; }

    private JobSchedule(TimeSpan interval, TimeSpan? preferredTimeOfDay = null, DayOfWeek? targetDayOfWeek = null, bool runOnStartup = false)
    {
        Interval = interval;
        PreferredTimeOfDay = preferredTimeOfDay;
        TargetDayOfWeek = targetDayOfWeek;
        RunOnStartup = runOnStartup;
    }

    public static JobSchedule FromInterval(TimeSpan interval, bool runOnStartup = false) =>
        new(interval, runOnStartup: runOnStartup);

    public static JobSchedule DailyAt(TimeSpan timeOfDay, bool runOnStartup = false) =>
        new(TimeSpan.FromDays(1), timeOfDay, runOnStartup: runOnStartup);

    public static JobSchedule EveryMinute(bool runOnStartup = false) =>
        new(TimeSpan.FromMinutes(1), runOnStartup: runOnStartup);

    public static JobSchedule WeeklyOn(DayOfWeek dayOfWeek, TimeSpan? timeOfDay = null, bool runOnStartup = false) =>
        new(TimeSpan.FromDays(7), timeOfDay ?? TimeSpan.Zero, dayOfWeek, runOnStartup: runOnStartup);

    public static JobSchedule EverySunday(TimeSpan? timeOfDay = null, bool runOnStartup = false) =>
        WeeklyOn(DayOfWeek.Sunday, timeOfDay, runOnStartup: runOnStartup);
}
