using System.Buffers.Binary;
using System.Net;

namespace Astrolabed.Dhcp.Protocol;

public class DhcpOption
{
    public DhcpOptionCode Code { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DhcpOption()
    {
    }

    public DhcpOption(DhcpOptionCode code, byte[] data)
    {
        Code = code;
        Data = data;
    }

    public static DhcpOption CreateByte(DhcpOptionCode code, byte value)
    {
        return new DhcpOption(code, new[] { value });
    }

    public static DhcpOption CreateIpAddress(DhcpOptionCode code, IPAddress address)
    {
        return new DhcpOption(code, address.GetAddressBytes());
    }

    public static DhcpOption CreateInt32(DhcpOptionCode code, int value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        return new DhcpOption(code, bytes);
    }
}
