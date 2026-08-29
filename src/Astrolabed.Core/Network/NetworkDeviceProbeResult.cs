namespace Astrolabed.Core.Network;

using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

/// <summary>
/// Contains collected network artifacts and probe responses for a target host.
/// </summary>
/// <param name="MacAddress">The physical address of the device.</param>
/// <param name="IpAddress">The IPv4/IPv6 address of the device.</param>
/// <param name="Hostname">Discovered host name via DNS/NetBIOS/DHCP.</param>
/// <param name="MdnsModelString">Model metadata string extracted from mDNS TXT records.</param>
/// <param name="SsdpServerHeader">Server header string extracted from UPnP/SSDP responses.</param>
/// <param name="DhcpVendorClass">DHCP Option 60 Vendor Class Identifier.</param>
/// <param name="TimeToLive">IP packet Time To Live value.</param>
/// <param name="OpenPorts">List of open TCP/UDP ports detected during scanning.</param>
public sealed record NetworkDeviceProbeResult(
    PhysicalAddress MacAddress,
    IPAddress IpAddress,
    string? Hostname = null,
    string? MdnsModelString = null,
    string? SsdpServerHeader = null,
    string? DhcpVendorClass = null,
    int? TimeToLive = null,
    IReadOnlyCollection<int>? OpenPorts = null);


