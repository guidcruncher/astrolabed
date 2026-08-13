using System.Net;
using System.Text;

namespace Astrolabed.Dhcp;

/// <summary>
/// Represents a parsed or outbound DHCP packet according to RFC 2131 and RFC 2132.
/// </summary>
public sealed class DhcpPacket
{
    public byte Op { get; set; }
    public byte Htype { get; set; }
    public byte Hlen { get; set; }
    public byte Hops { get; set; }
    public uint Xid { get; set; }
    public ushort Secs { get; set; }
    public ushort Flags { get; set; }
    public IPAddress Ciaddr { get; set; } = IPAddress.Any;
    public IPAddress Yiaddr { get; set; } = IPAddress.Any;
    public IPAddress Siaddr { get; set; } = IPAddress.Any;
    public IPAddress Giaddr { get; set; } = IPAddress.Any;
    public byte[] Chaddr { get; set; } = new byte[16];
    public List<DhcpOption> Options { get; } = [];

    /// <summary>
    /// Gets the DHCP Message Type (Option 53).
    /// </summary>
    public DhcpMessageType? GetMessageType()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 53);
        if (opt is null || opt.Data.Length == 0)
        {
            return null;
        }

        return (DhcpMessageType)opt.Data[0];
    }

    /// <summary>
    /// Gets the Requested IP Address (Option 50).
    /// </summary>
    public IPAddress? GetRequestedIp()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 50);
        return opt is { Data.Length: 4 } ? new IPAddress(opt.Data) : null;
    }

    /// <summary>
    /// Gets the DHCP Server Identifier (Option 54).
    /// </summary>
    public IPAddress? GetServerIdentifier()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 54);
        return opt is { Data.Length: 4 } ? new IPAddress(opt.Data) : null;
    }

    /// <summary>
    /// Gets the Client Hostname (Option 12).
    /// </summary>
    public string? GetHostName()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 12);
        if (opt is null || opt.Data.Length == 0)
        {
            return null;
        }

        return Encoding.UTF8.GetString(opt.Data).TrimEnd('\0');
    }

    /// <summary>
    /// Gets the Client FQDN string from Option 81 according to RFC 4702.
    /// Bytes 0-2 contain flags/RCODEs; domain name starts at byte 3.
    /// </summary>
    public string? GetFqdn()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 81);

        // Option 81 header requires 3 bytes for flags/RCODEs + at least 1 byte of payload
        if (opt is null || opt.Data.Length <= 3)
        {
            return null;
        }

        ReadOnlySpan<byte> fqdnPayload = opt.Data.AsSpan(3);
        return Encoding.UTF8.GetString(fqdnPayload).TrimEnd('\0');
    }

    public string GetFormatClientIdentifier()
    {
        var data = GetClientIdentifier();

        if (data is null || data.Length == 0)
        {
            return "(empty)";
        }

        byte type = data[0];
        ReadOnlySpan<byte> payload = data.AsSpan(1);

        // Type 1 = Ethernet MAC Address
        if (type == 1 && payload.Length == 6)
        {
            return Convert.ToHexStringLower(payload); // e.g. "001122334455"
        }

        // Type 0 = Explicit String Identifier
        if (type == 0 && payload.Length > 0)
        {
            return Encoding.UTF8.GetString(payload).TrimEnd('\0');
        }

        // Fallback for DUIDs and unknown types: Hex Representation
        return $"Type {type:X2}: 0x{Convert.ToHexStringLower(payload)}";
    }

    ///
    /// Gets the Client Identifier (Option 61) as a raw byte array or MAC/DUID payload.
    /// </summary>
    public byte[]? GetClientIdentifier()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 61);
        return opt?.Data;
    }

    /// <summary>
    /// Gets the Vendor Class Identifier (Option 60).
    /// </summary>
    public string? GetVendorClassIdentifier()
    {
        var opt = Options.FirstOrDefault(o => o.Code == 60);
        if (opt is null || opt.Data.Length == 0)
        {
            return null;
        }

        return Encoding.UTF8.GetString(opt.Data).TrimEnd('\0');
    }


    public IPAddress? GetIpAddressOption(DhcpOptionCode optionCode)
    {
        var opt = Options.FirstOrDefault(o => o.Code == (byte)optionCode);
        if (opt is null || opt.Data.Length == 0)
        {
            return null;
        }

        return opt.Data.Length == 4 ? new IPAddress(opt.Data) : null;
    }

    public string? GetStringOption(DhcpOptionCode optionCode)
    {
        var opt = Options.FirstOrDefault(o => o.Code == (byte)optionCode);
        if (opt is null || opt.Data.Length == 0)
        {
            return null;
        }

        return Encoding.UTF8.GetString(opt.Data).TrimEnd('\0');
    }

}
