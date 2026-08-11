using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Ntp;

public static class NtpReferenceId
{
    private const uint LocalClockRefId = 0x4C4F434C; // ASCII "LOCL"
    private const uint GpsRefId = 0x47505300;        // ASCII "GPS\0"

    public static uint FormatReferenceId(IPAddress? upstreamIp, int stratum)
    {
        if (stratum <= 1 || upstreamIp is null)
        {
            return stratum == 1 ? GpsRefId : LocalClockRefId;
        }

        if (upstreamIp.AddressFamily == AddressFamily.InterNetwork)
        {
            // IPv4: Write raw 4 octets in Big-Endian format
            byte[] bytes = upstreamIp.GetAddressBytes();
            return BinaryPrimitives.ReadUInt32BigEndian(bytes);
        }

        if (upstreamIp.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // IPv6: First 4 bytes of MD5 hash per RFC 5905
            byte[] bytes = upstreamIp.GetAddressBytes();
            byte[] hash = MD5.HashData(bytes);
            return BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(0, 4));
        }

        return LocalClockRefId;
    }
}

