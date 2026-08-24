using System.Buffers.Binary;
using System.Net;

namespace Astrolabed.Dhcp.Protocol;

/// <summary>
/// Represents an individual variable-length DHCP option parameter payload contained in a DHCP message.
/// </summary>
public class DhcpOption
{
    /// <summary>
    /// Gets or sets the DHCP option code tag.
    /// </summary>
    public DhcpOptionCode Code { get; set; }

    /// <summary>
    /// Gets or sets the binary payload data of the option.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Initializes a new instance of the <see cref="DhcpOption"/> class with default settings.
    /// </summary>
    public DhcpOption()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DhcpOption"/> class with a specified code and binary data payload.
    /// </summary>
    /// <param name="code">The DHCP option code tag.</param>
    /// <param name="data">The raw binary payload byte array.</param>
    public DhcpOption(DhcpOptionCode code, byte[] data)
    {
        Code = code;
        Data = data;
    }

    /// <summary>
    /// Factory method to construct a single byte <see cref="DhcpOption"/>.
    /// </summary>
    /// <param name="code">The DHCP option code tag.</param>
    /// <param name="value">The single byte value payload.</param>
    /// <returns>A new <see cref="DhcpOption"/> instance containing the byte value.</returns>
    public static DhcpOption CreateByte(DhcpOptionCode code, byte value)
    {
        return new DhcpOption(code, new[] { value });
    }

    /// <summary>
    /// Factory method to construct an IP address payload <see cref="DhcpOption"/>.
    /// </summary>
    /// <param name="code">The DHCP option code tag.</param>
    /// <param name="address">The IP address to store in the option payload.</param>
    /// <returns>A new <see cref="DhcpOption"/> instance containing the IP address bytes.</returns>
    public static DhcpOption CreateIpAddress(DhcpOptionCode code, IPAddress address)
    {
        return new DhcpOption(code, address.GetAddressBytes());
    }

    /// <summary>
    /// Factory method to construct a big-endian 32-bit signed integer payload <see cref="DhcpOption"/>.
    /// </summary>
    /// <param name="code">The DHCP option code tag.</param>
    /// <param name="value">The 32-bit integer to convert and store.</param>
    /// <returns>A new <see cref="DhcpOption"/> instance containing big-endian integer bytes.</returns>
    public static DhcpOption CreateInt32(DhcpOptionCode code, int value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        return new DhcpOption(code, bytes);
    }
}
