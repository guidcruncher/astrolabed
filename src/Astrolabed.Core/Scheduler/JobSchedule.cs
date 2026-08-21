namespace Astrolabed.Core.Scheduler;

public class JobSchedule
{
    public TimeSpan Interval { get; }
    public TimeSpan? PreferredTimeOfDay { get; }
    public DayOfWeek? TargetDayOfWeek { get; }

    private JobSchedule(TimeSpan interval, TimeSpan? preferredTimeOfDay = null, DayOfWeek? targetDayOfWeek = null)
    {
        Interval = interval;
        PreferredTimeOfDay = preferredTimeOfDay;
        TargetDayOfWeek = targetDayOfWeek;
    }

    public static JobSchedule FromInterval(TimeSpan interval) => new(interval);

    public static JobSchedule DailyAt(TimeSpan timeOfDay) => new(TimeSpan.FromDays(1), timeOfDay);

    public static JobSchedule EveryMinute() => new(TimeSpan.FromMinutes(1));

    public static JobSchedule WeeklyOn(DayOfWeek dayOfWeek, TimeSpan? timeOfDay = null) =>
        new(TimeSpan.FromDays(7), timeOfDay ?? TimeSpan.Zero, dayOfWeek);

    public static JobSchedule EverySunday(TimeSpan? timeOfDay = null) =>
        WeeklyOn(DayOfWeek.Sunday, timeOfDay);
}
