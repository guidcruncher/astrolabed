using System.Buffers.Binary;

namespace Astrolabed.Dhcp.Protocol;

/// <summary>
/// High-performance, zero-allocation binary encoder for RFC 2131 compliant DHCP network messages.
/// </summary>
public static class DhcpEncoder
{
    private const int MinimumHeaderSize = 236;
    private const int StandardHeaderSizeWithOptions = 240;
    private const int DefaultMaxMessageSize = 576;
    private const int SNameOffset = 44;
    private const int SNameLength = 64;
    private const int FileOffset = 108;
    private const int FileLength = 128;
    private const int OptionsOffset = 240;

    private static ReadOnlySpan<byte> MagicCookie => [0x63, 0x82, 0x53, 0x63];

    /// <summary>
    /// Encodes a structured <see cref="DhcpMessage"/> into an RFC 2131 compliant binary byte array payload.
    /// </summary>
    /// <param name="message">The DHCP message container to encode.</param>
    /// <returns>A byte array containing the serialized binary DHCP message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public static byte[] Encode(DhcpMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        ushort maxMessageSize = ExtractMaxMessageSize(message);
        byte[] buffer = new byte[maxMessageSize];
        int bytesWritten = EncodeToSpan(message, buffer, maxMessageSize);

        if (bytesWritten < buffer.Length)
        {
            Array.Resize(ref buffer, bytesWritten);
        }

        return buffer;
    }

    /// <summary>
    /// Encodes a structured <see cref="DhcpMessage"/> directly into a provided target byte span.
    /// </summary>
    /// <param name="message">The DHCP message container to encode.</param>
    /// <param name="destination">The destination byte span where binary packet bytes will be written.</param>
    /// <param name="maxMessageSize">The maximum allowed encoded packet size.</param>
    /// <returns>The actual number of bytes written to <paramref name="destination"/>.</returns>
    public static int EncodeToSpan(DhcpMessage message, Span<byte> destination, ushort maxMessageSize = DefaultMaxMessageSize)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (destination.Length < StandardHeaderSizeWithOptions)
        {
            throw new ArgumentException(
                $"Destination buffer size ({destination.Length} bytes) is insufficient for encoding a DHCP header.",
                nameof(destination));
        }

        destination.Clear();

        // Standard 236-byte Fixed Header
        destination[0] = (byte)message.Operation;
        destination[1] = message.HardwareType;
        destination[2] = message.HardwareAddressLength;
        destination[3] = message.Hops;

        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), message.TransactionId);
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(8, 2), message.Seconds);
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(10, 2), message.Flags);

        message.ClientIpAddress.TryWriteBytes(destination.Slice(12, 4), out _);
        message.YourIpAddress.TryWriteBytes(destination.Slice(16, 4), out _);
        message.ServerIpAddress.TryWriteBytes(destination.Slice(20, 4), out _);
        message.GatewayIpAddress.TryWriteBytes(destination.Slice(24, 4), out _);

        // Hardware Address (CHADDR)
        ReadOnlySpan<byte> chaddrSpan = message.ClientHardwareAddress;
        int chaddrLength = Math.Min(chaddrSpan.Length, 16);
        chaddrSpan[..chaddrLength].CopyTo(destination.Slice(28, chaddrLength));

        var normalOptions = new List<DhcpOption>();
        var fileOptions = new List<DhcpOption>();
        var snameOptions = new List<DhcpOption>();

        int totalOptionsLength = CalculateOptionsLength(message.Options);
        int estimatedSize = StandardHeaderSizeWithOptions + totalOptionsLength + 1; // +1 for END option
        byte overloadFlag = 0;

        if (estimatedSize > maxMessageSize || estimatedSize > DefaultMaxMessageSize)
        {
            PartitionOptions(message.Options, normalOptions, fileOptions, snameOptions, out overloadFlag);
        }
        else
        {
            normalOptions.AddRange(message.Options);
        }

        if (overloadFlag != 0)
        {
            normalOptions.Insert(0, DhcpOption.CreateByte(DhcpOptionCode.OptionOverload, overloadFlag));
            EncodeOptionsToBuffer(fileOptions, destination.Slice(FileOffset, FileLength));
            EncodeOptionsToBuffer(snameOptions, destination.Slice(SNameOffset, SNameLength));
        }

        // Magic Cookie Write (Bytes 236-239)
        MagicCookie.CopyTo(destination.Slice(MinimumHeaderSize, MagicCookieSize));

        int offset = OptionsOffset;

        foreach (DhcpOption option in normalOptions)
        {
            if (option.Code == DhcpOptionCode.Pad || option.Code == DhcpOptionCode.End)
            {
                continue;
            }

            int optionLength = 2 + option.Data.Length;
            if (offset + optionLength >= destination.Length || offset + optionLength >= maxMessageSize)
            {
                break;
            }

            destination[offset++] = (byte)option.Code;
            destination[offset++] = (byte)option.Data.Length;
            option.Data.CopyTo(destination.Slice(offset, option.Data.Length));
            offset += option.Data.Length;
        }

        // Write End Option
        if (offset < destination.Length && offset < maxMessageSize)
        {
            destination[offset++] = (byte)DhcpOptionCode.End;
        }

        return offset;
    }

    private static ushort ExtractMaxMessageSize(DhcpMessage message)
    {
        DhcpOption? maxMsgSizeOpt = message.Options.FirstOrDefault(o => o.Code == DhcpOptionCode.MaximumDhcpMessageSize);
        if (maxMsgSizeOpt != null && maxMsgSizeOpt.Data.Length >= 2)
        {
            ushort requestedSize = BinaryPrimitives.ReadUInt16BigEndian(maxMsgSizeOpt.Data);
            return Math.Max((ushort)DefaultMaxMessageSize, requestedSize);
        }

        return DefaultMaxMessageSize;
    }

    private static int CalculateOptionsLength(IEnumerable<DhcpOption> options)
    {
        return options.Where(o => o.Code != DhcpOptionCode.Pad && o.Code != DhcpOptionCode.End)
                      .Sum(o => 2 + o.Data.Length);
    }

    private static void PartitionOptions(
        List<DhcpOption> source,
        List<DhcpOption> normal,
        List<DhcpOption> file,
        List<DhcpOption> sname,
        out byte overloadFlag)
    {
        overloadFlag = 0;
        int currentNormalSize = 3; // Space for OptionOverload option

        foreach (DhcpOption option in source)
        {
            int optSize = 2 + option.Data.Length;

            if (currentNormalSize + optSize <= 308)
            {
                normal.Add(option);
                currentNormalSize += optSize;
            }
            else if (CalculateOptionsLength(file) + optSize <= 126)
            {
                file.Add(option);
                overloadFlag |= 1;
            }
            else if (CalculateOptionsLength(sname) + optSize <= 62)
            {
                sname.Add(option);
                overloadFlag |= 2;
            }
        }
    }

    private static void EncodeOptionsToBuffer(List<DhcpOption> options, Span<byte> destination)
    {
        if (options.Count == 0)
        {
            return;
        }

        int offset = 0;
        foreach (DhcpOption option in options)
        {
            if (offset + 2 + option.Data.Length >= destination.Length)
            {
                break;
            }

            destination[offset++] = (byte)option.Code;
            destination[offset++] = (byte)option.Data.Length;
            option.Data.CopyTo(destination.Slice(offset, option.Data.Length));
            offset += option.Data.Length;
        }

        if (offset < destination.Length)
        {
            destination[offset] = (byte)DhcpOptionCode.End;
        }
    }
}
