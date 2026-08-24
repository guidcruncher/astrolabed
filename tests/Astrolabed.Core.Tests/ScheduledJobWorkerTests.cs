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

    [Fact]
    public async Task ExecuteAsync_RunOnStartup_ExecutesImmediately()
    {
        MockScheduledJob.ExecutionCount = 0;
        MockScheduledJob.CurrentSchedule = JobSchedule.FromInterval(TimeSpan.FromHours(1), runOnStartup: true);

        ServiceCollection services = new ServiceCollection();
        services.AddScoped<MockScheduledJob>();
        ServiceProvider provider = services.BuildServiceProvider();

        FakeTimeProvider timeProvider = new();
        ScheduledJobWorker<MockScheduledJob> worker = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ScheduledJobWorker<MockScheduledJob>>.Instance,
            timeProvider);

        using CancellationTokenSource cts = new();
        Task workerTask = worker.StartAsync(cts.Token);

        await Task.Yield();
        Assert.Equal(1, MockScheduledJob.ExecutionCount);

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_AdvancesTime_TriggersScheduledRuns()
    {
        MockScheduledJob.ExecutionCount = 0;
        MockScheduledJob.CurrentSchedule = JobSchedule.FromInterval(TimeSpan.FromMinutes(10), runOnStartup: false);

        ServiceCollection services = new ServiceCollection();
        services.AddScoped<MockScheduledJob>();
        ServiceProvider provider = services.BuildServiceProvider();

        FakeTimeProvider timeProvider = new();
        ScheduledJobWorker<MockScheduledJob> worker = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ScheduledJobWorker<MockScheduledJob>>.Instance,
            timeProvider);

        using CancellationTokenSource cts = new();
        Task workerTask = worker.StartAsync(cts.Token);

        await Task.Yield();
        Assert.Equal(0, MockScheduledJob.ExecutionCount);

        timeProvider.Advance(TimeSpan.FromMinutes(10));
        await Task.Yield();

        Assert.Equal(1, MockScheduledJob.ExecutionCount);

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
    }
}
