// Target Framework: net10.0
namespace Astrolabed.Core.Network;

using System;

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

    /// <summary>
    /// Attempts to extract the 24-bit Organizationally Unique Identifier (OUI) prefix from a raw MAC address string in colon-delimited uppercase hexadecimal format.
    /// </summary>
    /// <param name="rawMacAddress">The raw MAC address string to parse.</param>
    /// <param name="ouiHex">When this method returns <c>true</c>, contains the extracted 3-byte OUI formatted as uppercase colon-separated hex pairs (e.g., "AA:BB:CC"); otherwise, an empty string.</param>
    /// <returns><c>true</c> if a valid 6-byte MAC address was parsed and the OUI extracted; otherwise, <c>false</c>.</returns>
    public static bool TryExtractOui(string rawMacAddress, out string ouiHex)
    {
        ouiHex = string.Empty;
        if (!TryParsePhysicalAddressBytes(rawMacAddress, out byte[] bytes))
        {
            return false;
        }
        ouiHex = $"{bytes[0]:X2}:{bytes[1]:X2}:{bytes[2]:X2}";
        return true;
    }

    /// <summary>
    /// Determines whether the specified MAC address uses a locally administered (randomized or private) Organizationally Unique Identifier (OUI).
    /// </summary>
    /// <param name="rawMacAddress">The raw MAC address string to analyze.</param>
    /// <returns>
    /// <c>true</c> if the MAC address is valid and has its Universally/Locally Administered (U/L) bit set to 1;
    /// otherwise, <c>false</c> if it is a universally administered hardware MAC address or invalid.
    /// </returns>
    public static bool IsRandomizedOui(string rawMacAddress)
    {
        if (!TryParsePhysicalAddressBytes(rawMacAddress, out byte[] bytes))
        {
            return false;
        }
        return (bytes[0] & 0x02) != 0;
    }

    /// <summary>
    /// Parses a raw MAC address string into a 6-byte array representing the physical network address.
    /// </summary>
    /// <param name="input">The raw MAC address string to parse (supports formats separated by colons, hyphens, periods, or delimiters-free).</param>
    /// <param name="addressBytes">When this method returns <c>true</c>, contains the 6-element byte array corresponding to the physical address; otherwise, an empty byte array.</param>
    /// <returns><c>true</c> if the input string represents a valid 48-bit MAC address; otherwise, <c>false</c>.</returns>
    public static bool TryParsePhysicalAddressBytes(string input, out byte[] addressBytes)
    {
        addressBytes = Array.Empty<byte>();
        ReadOnlySpan<char> span = input.AsSpan();
        Span<char> cleanedHex = stackalloc char[12];
        int hexCount = 0;
        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];
            if (Uri.IsHexDigit(c))
            {
                if (hexCount >= 12)
                {
                    return false;
                }
                cleanedHex[hexCount++] = c;
            }
            else if (c != ':' && c != '-' && c != '.')
            {
                return false;
            }
        }
        if (hexCount != 12)
        {
            return false;
        }
        try
        {
            addressBytes = Convert.FromHexString(cleanedHex);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
