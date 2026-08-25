using Astrolabed.Ntp.Protocol;

using Xunit;

namespace Astrolabed.Ntp.Tests;

public class NtpTimestampTests
{
    [Fact]
    public void FromDateTimeOffset_ToDateTimeOffset_RoundTripsAccurately()
    {
        // Arrange
        var expectedTime = DateTimeOffset.UtcNow;

        // Act
        var timestamp = NtpTimestamp.FromDateTimeOffset(expectedTime);
        var actualTime = timestamp.ToDateTimeOffset();

        // Assert
        Assert.True(Math.Abs((expectedTime - actualTime).TotalMilliseconds) < 1);
    }

    [Fact]
    public void FromDateTimeOffset_BeforeEpoch_ReturnsZero()
    {
        // Arrange
        var preEpochTime = new DateTimeOffset(1899, 12, 31, 23, 59, 59, TimeSpan.Zero);

        // Act
        var timestamp = NtpTimestamp.FromDateTimeOffset(preEpochTime);

        // Assert
        Assert.Equal(NtpTimestamp.Zero, timestamp);
    }

    [Fact]
    public void ReadAndWriteSpan_RoundTripsBinaryData()
    {
        // Arrange
        var original = new NtpTimestamp(0x12345678, 0x9ABCDEF0);
        Span<byte> buffer = stackalloc byte[8];

        // Act
        original.WriteToSpan(buffer);
        var restored = NtpTimestamp.ReadFromSpan(buffer);

        // Assert
        Assert.Equal(original, restored);
    }
}
