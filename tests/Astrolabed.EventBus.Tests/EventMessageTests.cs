namespace Astrolabed.EventBus.Tests;

using Astrolabed.EventBus;
using Microsoft.Extensions.Time.Testing;
using Xunit;

public class EventMessageTests
{
    [Fact]
    public void Constructor_NullPayload_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EventMessage<string>(null!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_NullPayload_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EventMessage<string>.Create(null!));
    }

    [Fact]
    public void Create_WithTimeProvider_UsesProviderUtcTime()
    {
        FakeTimeProvider timeProvider = new();
        DateTimeOffset expectedTime = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);
        timeProvider.SetUtcNow(expectedTime);

        EventMessage<string> message = EventMessage<string>.Create("TestPayload", timeProvider);

        Assert.Equal("TestPayload", message.Payload);
        Assert.Equal(expectedTime, message.Timestamp);
    }
}
