namespace Astrolabed.Core.Tests.Network;

using Astrolabed.Core.Network;

using Xunit;

public class MacAddressFormatterTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Format_NullOrWhitespaceInput_ReturnsEmptyString(string? input, string expected)
    {
        string result = MacAddressFormatter.Format(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("aabbccddeeff", "AA:BB:CC:DD:EE:FF")]
    [InlineData("AA-BB-CC-DD-EE-FF", "AA:BB:CC:DD:EE:FF")]
    [InlineData("aa:bb:cc:dd:ee:ff", "AA:BB:CC:DD:EE:FF")]
    [InlineData("001122334455", "00:11:22:33:44:55")]
    public void Format_ValidMacAddress_ReturnsFormattedString(string input, string expected)
    {
        string result = MacAddressFormatter.Format(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("aabbccddeef", "AABBCCDDEEF")]
    [InlineData("aabbccddeeff00", "AABBCCDDEEFF00")]
    [InlineData("invalid_mac", "INVALID_MAC")]
    public void Format_InvalidHexCount_ReturnsTrimmedUppercaseFallback(string input, string expected)
    {
        string result = MacAddressFormatter.Format(input);

        Assert.Equal(expected, result);
    }
}
