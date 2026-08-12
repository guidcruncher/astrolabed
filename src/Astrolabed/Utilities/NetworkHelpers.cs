using System;
using System.Net;
using System.Net.Sockets;

namespace Astrolabed.Utilities;

public static class NetworkHelpers
{
    /// <summary>
    /// Converts a CIDR notation string (e.g., "192.168.1.0/24") to an IPAddress netmask.
    /// </summary>
    public static IPAddress CidrToNetmask(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
        {
            throw new ArgumentNullException(nameof(cidr));
        }

        string[] parts = cidr.Split('/');
        if (parts.Length != 2)
        {
            throw new FormatException("Invalid CIDR format. Expected format 'IP/Prefix' (e.g., '192.168.1.0/24').");
        }

        if (!IPAddress.TryParse(parts[0], out IPAddress? ip) || ip.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new FormatException("Invalid IPv4 address in CIDR string.");
        }

        if (!int.TryParse(parts[1], out int prefixLength) || prefixLength < 0 || prefixLength > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(cidr), "Prefix length must be an integer between 0 and 32.");
        }

        return PrefixLengthToNetmask(prefixLength);
    }

    /// <summary>
    /// Converts an IPv4 prefix length (0-32) to an IPAddress netmask.
    /// </summary>
    public static IPAddress PrefixLengthToNetmask(int prefixLength)
    {
        if (prefixLength < 0 || prefixLength > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(prefixLength), "Prefix length must be between 0 and 32.");
        }

        uint mask = prefixLength == 0 ? 0u : 0xFFFFFFFFu << (32 - prefixLength);

        byte[] maskBytes = new byte[]
        {
            (byte)(mask >> 24),
            (byte)(mask >> 16),
            (byte)(mask >> 8),
            (byte)mask
        };

        return new IPAddress(maskBytes);
    }
}

