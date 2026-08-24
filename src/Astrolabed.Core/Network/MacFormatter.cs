namespace Astrolabed.Core.Network;

/// <summary>
/// Provides utility methods for parsing and formatting MAC address strings into standardized colon-delimited formats.
/// </summary>
public static class MacAddressFormatter
{
    /// <summary>
    /// Normalizes and formats a raw MAC address string into standard colon-separated uppercase hex pairs (e.g., "AA:BB:CC:DD:EE:FF").
    /// </summary>
    /// <param name="macAddress">The raw MAC address string to format.</param>
    /// <returns>
    /// A formatted colon-delimited MAC address string if valid; 
    /// an empty string if <paramref name="macAddress"/> is <c>null</c> or whitespace; 
    /// or a trimmed, uppercase fallback string if the input does not contain exactly 12 hexadecimal characters.
    /// </returns>
    public static string Format(string? macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return string.Empty;
        }

        Span<char> hex = stackalloc char[12];
        int count = 0;

        foreach (char c in macAddress)
        {
            if (char.IsAsciiHexDigit(c))
            {
                if (count < 12)
                {
                    hex[count++] = char.ToUpperInvariant(c);
                }
                else
                {
                    count++;
                    break;
                }
            }
        }

        if (count != 12)
        {
            return macAddress.Trim().ToUpperInvariant();
        }

        return $"{hex[0]}{hex[1]}:{hex[2]}{hex[3]}:{hex[4]}{hex[5]}:{hex[6]}{hex[7]}:{hex[8]}{hex[9]}:{hex[10]}{hex[11]}";
    }
}
