namespace Astrolabed.Core.Network;

using System;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Provides extension methods for <see cref="IPAddress"/> objects.
/// </summary>
public static class IpAddressExtensions
{
    private const string InAddrArpaSuffix = "in-addr.arpa";
    private const string Ip6ArpaSuffix = "ip6.arpa";

    /// <summary>
    /// Converts an <see cref="IPAddress"/> to its reverse DNS PTR question name format.
    /// </summary>

    /// <param name="address">The IP address to convert.</param>
    /// <param name="includeTrailingDot">Whether to include a trailing dot at the end of the domain name (e.g., FQDN format).</param>
    /// <param name="useUpperCaseHex">Whether IPv6 nibbles should be formatted in uppercase.</param>
    /// <returns>A string formatted for reverse DNS PTR queries.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="address"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the address family is not IPv4 or IPv6.</exception>
    public static string ToPtrFormat(
        this IPAddress address,
        bool includeTrailingDot = false,
        bool useUpperCaseHex = false)
    {
        ArgumentNullException.ThrowIfNull(address);

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => ConvertIPv4(address, includeTrailingDot),
            AddressFamily.InterNetworkV6 => ConvertIPv6(address, includeTrailingDot, useUpperCaseHex),
            _ => ThrowUnsupportedAddressFamily(address)
        };
    }

    private static string ConvertIPv4(IPAddress address, bool includeTrailingDot)
    {

        ReadOnlySpan<byte> bytes = address.GetAddressBytes();
        string suffix = includeTrailingDot ? $"{InAddrArpaSuffix}." : InAddrArpaSuffix;

        return $"{bytes[3]}.{bytes[2]}.{bytes[1]}.{bytes[0]}.{suffix}";
    }

    private static string ConvertIPv6(IPAddress address, bool includeTrailingDot, bool useUpperCaseHex)
    {

        ReadOnlySpan<byte> bytes = address.GetAddressBytes();
        string suffix = includeTrailingDot ? $"{Ip6ArpaSuffix}." : Ip6ArpaSuffix;

        // 16 bytes = 32 nibbles, each followed by a dot (64 chars total) + suffix length.
        int requiredLength = 64 + suffix.Length;

        return string.Create(
            requiredLength,
            (Bytes: bytes.ToArray(), Suffix: suffix, UpperCase: useUpperCaseHex),
            static (span, state) =>
            {
                ReadOnlySpan<byte> bytesSpan = state.Bytes;
                ReadOnlySpan<char> hexAlphabet = state.UpperCase
                    ? "0123456789ABCDEF"
                    : "0123456789abcdef";

                int position = 0;

                // Process bytes in reverse order for reverse DNS lookup
                for (int i = bytesSpan.Length - 1; i >= 0; i--)
                {
                    byte b = bytesSpan[i];

                    // Lower nibble comes first in PTR notation
                    int lowNibble = b & 0x0F;
                    int highNibble = (b >> 4) & 0x0F;

                    span[position++] = hexAlphabet[lowNibble];
                    span[position++] = '.';

                    span[position++] = hexAlphabet[highNibble];
                    span[position++] = '.';
                }

                state.Suffix.AsSpan().CopyTo(span[position..]);
            });
    }

    private static string ThrowUnsupportedAddressFamily(IPAddress address)
    {
        throw new ArgumentException($"Unsupported IP address family: {address.AddressFamily}", nameof(address));
    }
}
