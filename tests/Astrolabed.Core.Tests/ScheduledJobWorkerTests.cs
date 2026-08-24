namespace Astrolabed.Core.Tests.Scheduler;

using Astrolabed.Core.Scheduler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

public class ScheduledJobWorkerTests
{
    public sealed class MockScheduledJob : IScheduledJob
    {
        public static int ExecutionCount { get; set; }
        public static JobSchedule CurrentSchedule { get; set; } = JobSchedule.FromInterval(TimeSpan.FromSeconds(5));

        public JobSchedule Schedule => CurrentSchedule;

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ScheduledJobWorker<MockScheduledJob>(
            null!,
            NullLogger<ScheduledJobWorker<MockScheduledJob>>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        IServiceCollection services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        Assert.Throws<ArgumentNullException>(() => new ScheduledJobWorker<MockScheduledJob>(
            scopeFactory,
            null!));
    }

}
