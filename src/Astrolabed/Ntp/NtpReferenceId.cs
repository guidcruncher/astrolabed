using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

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
            byte[] bytes = upstreamIp.GetAddressBytes();
            return BinaryPrimitives.ReadUInt32BigEndian(bytes);
        }

        if (upstreamIp.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] bytes = upstreamIp.GetAddressBytes();
            byte[] hash = MD5.HashData(bytes);
            return BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(0, 4));
        }

        return LocalClockRefId;
    }
}

