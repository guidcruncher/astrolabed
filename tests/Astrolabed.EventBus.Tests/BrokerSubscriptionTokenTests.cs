namespace Astrolabed.EventBus.Tests;

using Astrolabed.EventBus;

using Xunit;

public class BrokerSubscriptionTokenTests
{
    [Fact]
    public void Constructor_NullActionDelegate_ThrowsArgumentNullException()
    {
        Action action = null!;
        Assert.Throws<ArgumentNullException>(() => new BrokerSubscriptionToken(action));
    }

    [Fact]
    public void Constructor_NullFuncDelegate_ThrowsArgumentNullException()
    {
        Func<ValueTask> func = null!;
        Assert.Throws<ArgumentNullException>(() => new BrokerSubscriptionToken(func));
    }

    [Fact]
    public void Dispose_ExecutesUnsubscribeDelegateOnce()
    {
        int count = 0;
        BrokerSubscriptionToken token = new(() =>
        {
            Interlocked.Increment(ref count);
            return ValueTask.CompletedTask;
        });

        token.Dispose();
        token.Dispose();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DisposeAsync_ExecutesUnsubscribeDelegateOnce()
    {
        int count = 0;
        BrokerSubscriptionToken token = new(() =>
        {
            Interlocked.Increment(ref count);
            return ValueTask.CompletedTask;
        });

        await token.DisposeAsync();
        await token.DisposeAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Dispose_And_DisposeAsync_CalledConcurrently_ExecutesOnlyOnce()
    {
        int count = 0;
        BrokerSubscriptionToken token = new(() =>
        {
            Interlocked.Increment(ref count);
            return ValueTask.CompletedTask;
        });

        Task syncTask = Task.Run(() => token.Dispose());
        ValueTask asyncTask = token.DisposeAsync();

        await syncTask;
        await asyncTask;

        Assert.Equal(1, count);
    }
}
