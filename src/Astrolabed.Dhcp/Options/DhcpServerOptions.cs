using System.Buffers.Binary;
using System.Net;

using Astrolabed.Core.Options;

namespace Astrolabed.Dhcp.Options;

public class DhcpServerOptions
{
    public const string Position = "DhcpServer";

    public AddressOptions ListenAddress { get; set; } = new();
    public bool TestMode { get; set; } = false;
    public int TestPort { get; set; } = 6767;
    public string ServerIp { get; set; } = "192.168.1.1";
    public string SubnetMask { get; set; } = "255.255.255.0";
    public string Router { get; set; } = "192.168.1.1";
    public string DnsServer { get; set; } = "192.168.1.1";
    public string NtpServer { get; set; } = "192.168.1.1";
    public string DomainName { get; set; } = "local";
    public string StartIpAddress { get; set; } = "192.168.1.100";
    public string EndIpAddress { get; set; } = "192.168.1.200";
    public int LeaseTimeSeconds { get; set; } = 86400;

    public IPAddress GetServerIp() => IPAddress.Parse(ServerIp);
    public IPAddress GetSubnetMask() => IPAddress.Parse(SubnetMask);
    public IPAddress GetRouter() => IPAddress.Parse(Router);
    public IPAddress GetDnsServer() => IPAddress.Parse(DnsServer);
    public IPAddress GetNtpServer() => IPAddress.Parse(NtpServer);
    public IPAddress GetStartIpAddress() => IPAddress.Parse(StartIpAddress);
    public IPAddress GetEndIpAddress() => IPAddress.Parse(EndIpAddress);

    public bool IsIpInPool(IPAddress address)
    {
        byte[] start = GetStartIpAddress().GetAddressBytes();
        byte[] end = GetEndIpAddress().GetAddressBytes();
        byte[] target = address.GetAddressBytes();

        if (start.Length != 4 || target.Length != 4)
        {
            return false;
        }

        uint startNum = BinaryPrimitives.ReadUInt32BigEndian(start);
        uint endNum = BinaryPrimitives.ReadUInt32BigEndian(end);
        uint targetNum = BinaryPrimitives.ReadUInt32BigEndian(target);

        return targetNum >= startNum && targetNum <= endNum;
    }
}
