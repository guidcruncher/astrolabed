using System.Net;

namespace Astrolabed.Dhcp.Options;

public class DhcpServerOptions
{
    public const string Position = "DhcpServer";

    public string ListenAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 67;
    public bool TestMode { get; set; } = false;
    public int TestPort { get; set; } = 6767;
    public string ServerIp { get; set; } = "192.168.1.1";
    public string SubnetMask { get; set; } = "255.255.255.0";
    public string Router { get; set; } = "192.168.1.1";
    public string DnsServer { get; set; } = "192.168.1.1";
    public string StartIpAddress { get; set; } = "192.168.1.100";
    public string EndIpAddress { get; set; } = "192.168.1.200";
    public int LeaseTimeSeconds { get; set; } = 86400;

    public IPAddress GetServerIp() => IPAddress.Parse(ServerIp);
    public IPAddress GetSubnetMask() => IPAddress.Parse(SubnetMask);
    public IPAddress GetRouter() => IPAddress.Parse(Router);
    public IPAddress GetDnsServer() => IPAddress.Parse(DnsServer);
    public IPAddress GetStartIpAddress() => IPAddress.Parse(StartIpAddress);
    public IPAddress GetEndIpAddress() => IPAddress.Parse(EndIpAddress);
}
