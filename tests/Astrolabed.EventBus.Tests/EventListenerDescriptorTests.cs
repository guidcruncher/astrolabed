namespace Astrolabed.EventBus.Tests;

using Astrolabed.EventBus;
using Xunit;

public class EventListenerDescriptorTests
{
    [Fact]
    public void Constructor_NullMessageType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EventListenerDescriptor(null!));
    }

    [Fact]
    public void Equals_SameMessageType_ReturnsTrue()
    {
        EventListenerDescriptor d1 = new(typeof(string));
        EventListenerDescriptor d2 = new(typeof(string));

        Assert.True(d1.Equals(d2));
        Assert.True(d1.Equals((object)d2));
        Assert.True(d1 == d2);
        Assert.False(d1 != d2);
        Assert.Equal(d1.GetHashCode(), d2.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentMessageType_ReturnsFalse()
    {
        EventListenerDescriptor d1 = new(typeof(string));
        EventListenerDescriptor d2 = new(typeof(int));

        Assert.False(d1.Equals(d2));
        Assert.False(d1 == d2);
        Assert.True(d1 != d2);
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        EventListenerDescriptor d1 = new(typeof(string));

        Assert.False(d1.Equals(null));
        Assert.False(d1 == null);
        Assert.True(d1 != null);
    }
}
