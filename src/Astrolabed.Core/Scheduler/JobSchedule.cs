namespace Astrolabed.Core.Scheduler;

public class JobSchedule
{
    public TimeSpan Interval { get; }
    public TimeSpan? PreferredTimeOfDay { get; }

    private JobSchedule(TimeSpan interval, TimeSpan? preferredTimeOfDay = null)
    {
        Interval = interval;
        PreferredTimeOfDay = preferredTimeOfDay;
    }

    public static JobSchedule FromInterval(TimeSpan interval) => new(interval);

    public static JobSchedule DailyAt(TimeSpan timeOfDay) => new(TimeSpan.FromDays(1), timeOfDay);

    public static JobSchedule EveryMinute() => new(TimeSpan.FromMinutes(1));

    public static JobSchedule EveryHour() => new(TimeSpan.FromMinutes(60));
}
