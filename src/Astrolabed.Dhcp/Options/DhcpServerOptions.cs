using System.Buffers.Binary;
using System.Net;

using Astrolabed.Core.Options;

namespace Astrolabed.Dhcp.Options;

/// <summary>
/// Configuration options for setting up the DHCP server service, binding ports, subnets, and address assignment ranges.
/// </summary>
public class DhcpServerOptions
{
    /// <summary>
    /// The configuration section path key within configuration source providers.
    /// </summary>
    public const string Position = "DhcpServer";

    /// <summary>
    /// Gets or sets the network binding options for the listening socket.
    /// </summary>
    public AddressOptions ListenAddress { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether test execution mode is enabled for non-root/test environment bindings.
    /// </summary>
    public bool TestMode { get; set; } = false;

    /// <summary>
    /// Gets or sets the alternative port used when running under <see cref="TestMode"/>.
    /// </summary>
    /// <value>Defaults to <c>6767</c>.</value>
    public int TestPort { get; set; } = 6767;

    /// <summary>
    /// Gets or sets the IP address string of the local DHCP server instance.
    /// </summary>
    public string ServerIp { get; set; } = "192.168.1.1";

    /// <summary>
    /// Gets or sets the subnet mask string assigned to network clients.
    /// </summary>
    public string SubnetMask { get; set; } = "255.255.255.0";

    /// <summary>
    /// Gets or sets the default gateway router IP address string for network clients.
    /// </summary>
    public string Router { get; set; } = "192.168.1.1";

    /// <summary>
    /// Gets or sets the DNS resolver server IP address string advertised to clients.
    /// </summary>
    public string DnsServer { get; set; } = "192.168.1.1";

    /// <summary>
    /// Gets or sets the NTP time server IP address string advertised to clients.
    /// </summary>
    public string NtpServer { get; set; } = "192.168.1.1";

    /// <summary>
    /// Gets or sets the local domain suffix name advertised in Option 15.
    /// </summary>
    public string DomainName { get; set; } = "local";

    /// <summary>
    /// Gets or sets the starting IPv4 address string of the allocation pool range.
    /// </summary>
    public string StartIpAddress { get; set; } = "192.168.1.100";

    /// <summary>
    /// Gets or sets the ending IPv4 address string of the allocation pool range.
    /// </summary>
    public string EndIpAddress { get; set; } = "192.168.1.200";

    /// <summary>
    /// Gets or sets the standard lease duration granted to clients in seconds.
    /// </summary>
    /// <value>Lease duration in seconds. Defaults to <c>86400</c> (24 hours).</value>
    public int LeaseTimeSeconds { get; set; } = 86400;

    /// <summary>
    /// Parses and returns <see cref="ServerIp"/> as an <see cref="IPAddress"/> instance.
    /// </summary>
    /// <returns>A parsed <see cref="IPAddress"/>.</returns>
    public IPAddress GetServerIp() => IPAddress.Parse(ServerIp);

    /// <summary>
    /// Parses and returns <see cref="SubnetMask"/> as an <see cref="IPAddress"/> instance.
    /// </summary>
    /// <returns>A parsed <see cref="IPAddress"/>.</returns>
    public IPAddress GetSubnetMask() => IPAddress.Parse(SubnetMask);

    /// <summary>
    /// Parses and returns <see cref="Router"/> as an <see cref="IPAddress"/> instance.
    /// </summary>
    /// <returns>A parsed <see cref="IPAddress"/>.</returns>
    public IPAddress GetRouter() => IPAddress.Parse(Router);

    /// <summary>
    /// Parses and returns <see cref="DnsServer"/> as an <see cref="IPAddress"/> instance.
    /// </summary>
    /// <returns>A parsed <see cref="IPAddress"/>.</returns>
    public IPAddress GetDnsServer() => IPAddress.Parse(DnsServer);

    /// <summary>
    /// Parses and returns <see cref="NtpServer"/> as an <see cref="IPAddress"/> instance.
    /// </summary>
    /// <returns>A parsed <see cref="IPAddress"/>.</returns>
    public IPAddress GetNtpServer() => IPAddress.Parse(NtpServer);

    /// <summary>
    /// Parses and returns <see cref="StartIpAddress"/> as an <see cref="IPAddress"/> instance.
    /// </summary>
    /// <returns>A parsed <see cref="IPAddress"/>.</returns>
    public IPAddress GetStartIpAddress() => IPAddress.Parse(StartIpAddress);

    /// <summary>
    /// Parses and returns <see cref="EndIpAddress"/> as an <see cref="IPAddress"/> instance.
    /// </summary>
    /// <returns>A parsed <see cref="IPAddress"/>.</returns>
    public IPAddress GetEndIpAddress() => IPAddress.Parse(EndIpAddress);

    /// <summary>
    /// Evaluates whether an IPv4 address resides within the configured start and end pool boundaries.
    /// </summary>
    /// <param name="address">The target IP address to check.</param>
    /// <returns><c>true</c> if <paramref name="address"/> is within range; otherwise <c>false</c>.</returns>
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
