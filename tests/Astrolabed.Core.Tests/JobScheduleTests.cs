namespace Astrolabed.Core.Tests.Scheduler;

using Astrolabed.Core.Scheduler;
using Xunit;

public class JobScheduleTests
{
    [Fact]
    public void FromInterval_ValidInterval_SetsPropertiesCorrectly()
    {
        TimeSpan interval = TimeSpan.FromMinutes(15);

        JobSchedule schedule = JobSchedule.FromInterval(interval, runOnStartup: true);

        Assert.Equal(interval, schedule.Interval);
        Assert.True(schedule.RunOnStartup);
        Assert.Null(schedule.PreferredTimeOfDay);
        Assert.Null(schedule.TargetDayOfWeek);
    }

    [Fact]
    public void DailyAt_ValidTimeOfDay_SetsPropertiesCorrectly()
    {
        TimeSpan timeOfDay = new(2, 30, 0);

        JobSchedule schedule = JobSchedule.DailyAt(timeOfDay);

        Assert.Equal(TimeSpan.FromDays(1), schedule.Interval);
        Assert.Equal(timeOfDay, schedule.PreferredTimeOfDay);
        Assert.False(schedule.RunOnStartup);
    }

    [Fact]
    public void DailyAt_InvalidTimeOfDay_ThrowsArgumentOutOfRangeException()
    {
        TimeSpan invalidTime = TimeSpan.FromHours(25);

        Assert.Throws<ArgumentOutOfRangeException>(() => JobSchedule.DailyAt(invalidTime));
    }

    [Fact]
    public void EveryMinute_SetsIntervalToOneMinute()
    {
        JobSchedule schedule = JobSchedule.EveryMinute();

        Assert.Equal(TimeSpan.FromMinutes(1), schedule.Interval);
    }

    [Fact]
    public void WeeklyOn_SetsTargetDayAndInterval()
    {
        TimeSpan timeOfDay = new(4, 0, 0);

        JobSchedule schedule = JobSchedule.WeeklyOn(DayOfWeek.Wednesday, timeOfDay);

        Assert.Equal(TimeSpan.FromDays(7), schedule.Interval);
        Assert.Equal(DayOfWeek.Wednesday, schedule.TargetDayOfWeek);
        Assert.Equal(timeOfDay, schedule.PreferredTimeOfDay);
    }

    [Fact]
    public void EverySunday_SetsDayToSunday()
    {
        JobSchedule schedule = JobSchedule.EverySunday();

        Assert.Equal(TimeSpan.FromDays(7), schedule.Interval);
        Assert.Equal(DayOfWeek.Sunday, schedule.TargetDayOfWeek);
    }
}
