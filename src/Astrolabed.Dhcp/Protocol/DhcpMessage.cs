using System.Net;

namespace Astrolabed.Dhcp.Protocol;

public class DhcpMessage
{
    public DhcpOpCode Operation { get; set; } = DhcpOpCode.BootRequest;
    public byte HardwareType { get; set; } = 1;
    public byte HardwareAddressLength { get; set; } = 6;
    public byte Hops { get; set; }
    public uint TransactionId { get; set; }
    public ushort Seconds { get; set; }
    public ushort Flags { get; set; }
    public IPAddress ClientIpAddress { get; set; } = IPAddress.Any;
    public IPAddress YourIpAddress { get; set; } = IPAddress.Any;
    public IPAddress ServerIpAddress { get; set; } = IPAddress.Any;
    public IPAddress GatewayIpAddress { get; set; } = IPAddress.Any;
    public byte[] ClientHardwareAddress { get; set; } = new byte[16];
    public string ServerHostName { get; set; } = string.Empty;
    public string BootFileName { get; set; } = string.Empty;
    public List<DhcpOption> Options { get; set; } = new();

    public DhcpMessageType? GetMessageType()
    {
        var option = Options.FirstOrDefault(o => o.Code == DhcpOptionCode.DhcpMessageType);
        if (option != null && option.Data.Length > 0)
        {
            return (DhcpMessageType)option.Data[0];
        }
        return null;
    }
}
