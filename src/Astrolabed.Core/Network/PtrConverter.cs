namespace Astrolabed.Core.Network;

using System;
using System.Globalization;
using System.Net;

/// <summary>
/// Provides high-performance utility methods to parse PTR address strings into IPAddress objects.
/// </summary>
public static class PtrConverter
{
    private const string IPv4Suffix = ".in-addr.arpa";
    private const string IPv6Suffix = ".ip6.arpa";

    /// <summary>
    /// Parses a reverse DNS PTR string (e.g., "1.1.168.192.in-addr.arpa" or "b.a.9.8...ip6.arpa") into an IPAddress.
    /// </summary>
    /// <param name="ptrAddress">The PTR domain name string.</param>
    /// <returns>The converted <see cref="IPAddress"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when ptrAddress is null.</exception>
    /// <exception cref="FormatException">Thrown when ptrAddress is not a valid reverse lookup PTR domain.</exception>
    public static IPAddress ToIPAddress(string ptrAddress)
    {
        ArgumentNullException.ThrowIfNull(ptrAddress);

        if (TryParse(ptrAddress.AsSpan(), out IPAddress? address))
        {
            return address;
        }

        throw new FormatException($"The provided string '{ptrAddress}' is not a valid PTR address.");
    }

    /// <summary>
    /// Tries to parse a reverse DNS PTR span into an IPAddress without throwing exceptions.
    /// </summary>
    /// <param name="ptrSpan">The read-only span containing the PTR string.</param>
    /// <param name="address">The resulting IPAddress, or null if parsing failed.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(ReadOnlySpan<char> ptrSpan, out IPAddress? address)
    {
        address = null;
        ReadOnlySpan<char> trimmed = ptrSpan.Trim().TrimEnd('.');

        if (trimmed.EndsWith(IPv4Suffix, StringComparison.OrdinalIgnoreCase))
        {
            return TryParseIPv4Ptr(trimmed[..^IPv4Suffix.Length], out address);
        }

        if (trimmed.EndsWith(IPv6Suffix, StringComparison.OrdinalIgnoreCase))
        {
            return TryParseIPv6Ptr(trimmed[..^IPv6Suffix.Length], out address);
        }

        return false;
    }

    private static bool TryParseIPv4Ptr(ReadOnlySpan<char> labelsSpan, out IPAddress? address)
    {
        address = null;
        Span<byte> bytes = stackalloc byte[4];
        int octetCount = 0;

        // Process labels in reverse order since in-addr.arpa stores IP octets backward
        while (labelsSpan.Length > 0)
        {
            if (octetCount >= 4)
            {
                return false; // Too many octets
            }

            int lastDotIndex = labelsSpan.LastIndexOf('.');
            ReadOnlySpan<char> label = lastDotIndex < 0 ? labelsSpan : labelsSpan[(lastDotIndex + 1)..];

            if (!byte.TryParse(label, CultureInfo.InvariantCulture, out byte octet))
            {
                return false;
            }

            bytes[octetCount++] = octet;

            labelsSpan = lastDotIndex < 0 ? ReadOnlySpan<char>.Empty : labelsSpan[..lastDotIndex];
        }

        if (octetCount != 4)
        {
            return false; // Incomplete address
        }

        address = new IPAddress(bytes);
        return true;
    }

    private static bool TryParseIPv6Ptr(ReadOnlySpan<char> labelsSpan, out IPAddress? address)
    {
        address = null;
        Span<byte> bytes = stackalloc byte[16];
        int nibbleCount = 0;

        // Process labels in reverse order (each label is a single hex nibble)
        while (labelsSpan.Length > 0)
        {
            if (nibbleCount >= 32)
            {
                return false; // Too many nibbles for IPv6
            }

            int lastDotIndex = labelsSpan.LastIndexOf('.');
            ReadOnlySpan<char> label = lastDotIndex < 0 ? labelsSpan : labelsSpan[(lastDotIndex + 1)..];

            if (label.Length != 1 || !Uri.IsHexDigit(label[0]))
            {
                return false;
            }

            byte nibbleVal = (byte)Uri.FromHex(label[0]);

            // Two hex nibbles make up one byte in IPv6 memory layout
            int byteIndex = nibbleCount / 2;
            if (nibbleCount % 2 == 0)
            {
                bytes[byteIndex] = (byte)(nibbleVal << 4);
            }
            else
            {
                bytes[byteIndex] |= nibbleVal;
            }

            nibbleCount++;
            labelsSpan = lastDotIndex < 0 ? ReadOnlySpan<char>.Empty : labelsSpan[..lastDotIndex];
        }

        if (nibbleCount != 32)
        {
            return false; // Must have exactly 32 nibbles for IPv6
        }

        address = new IPAddress(bytes);
        return true;
    }
}
