namespace Astrolabed.Core.Network;

using System;
using System.Linq;
using System.Net.NetworkInformation;

using Astrolabed.Core.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Implements multi-stage device classification utilizing <see cref="IMacVendorLookupService"/> and network layer heuristics.
/// </summary>
public class NetworkDeviceClassifier : INetworkDeviceClassifier
{
    private readonly IMacVendorLookupService _macVendorLookup;
    private readonly ILogger<NetworkDeviceClassifier> _logger;
    private readonly NetworkScannerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkDeviceClassifier"/> class.
    /// </summary>
    /// <param name="macVendorLookup">The MAC address vendor lookup service instance.</param>
    /// <param name="logger">The logger for diagnostics and tracing.</param>
    /// <param name="options">The network scanner options containing gateway and environment settings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="macVendorLookup"/>, <paramref name="logger"/>, or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public NetworkDeviceClassifier(
        IMacVendorLookupService macVendorLookup,
        ILogger<NetworkDeviceClassifier> logger,
        IOptions<NetworkScannerOptions> options)
    {
        _macVendorLookup = macVendorLookup ?? throw new ArgumentNullException(nameof(macVendorLookup));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Analyzes collected network probe details and resolves the target device category using MAC vendor lookups and multi-layer protocol signatures.
    /// </summary>
    /// <param name="probeResult">The collected network artifacts and probe signatures for the target device.</param>
    /// <returns>The resolved <see cref="DeviceType"/> matching the device characteristics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="probeResult"/> is <see langword="null"/>.</exception>
    public DeviceType ClassifyDevice(NetworkDeviceProbeResult probeResult)
    {
        ArgumentNullException.ThrowIfNull(probeResult);

        string macString = FormatMacAddress(probeResult.MacAddress);
        string vendorName = string.Empty;

        if (_macVendorLookup.TryGetVendor(macString, out MacVendorInfo? vendorInfo) && vendorInfo != null)
        {
            vendorName = vendorInfo.VendorName;
            _logger.LogDebug("Resolved MAC {MacAddress} to Vendor '{VendorName}' (BlockType: {BlockType})",
                macString, vendorName, vendorInfo.BlockType ?? "N/A");
        }
        else
        {
            _logger.LogDebug("MAC vendor lookup failed or returned unknown for MAC {MacAddress}", macString);
        }

        if (IsRouter(probeResult, vendorName))
        {
            return DeviceType.Router;
        }

        if (!string.IsNullOrWhiteSpace(probeResult.MdnsModelString))
        {
            if (probeResult.MdnsModelString.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.iPhone;
            }

            if (probeResult.MdnsModelString.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.iPad;
            }
        }

        if (IsNintendo(vendorName, probeResult)) return DeviceType.Nintendo;
        if (IsPlaystation(vendorName, probeResult)) return DeviceType.Playstation;
        if (IsXbox(vendorName, probeResult)) return DeviceType.XBOX;
        if (IsSmartTv(vendorName, probeResult)) return DeviceType.SmartTV;
        if (IsWindowsPc(probeResult)) return DeviceType.PC;
        if (IsAndroid(probeResult)) return DeviceType.Android;

        if (vendorName.Contains("Apple", StringComparison.OrdinalIgnoreCase))
        {
            return DeviceType.Apple;
        }

        if (IsIoTDevice(vendorName, probeResult)) return DeviceType.IoT;
        if (IsLinuxDevice(probeResult)) return DeviceType.Linux;

        return DeviceType.Unknown;
    }

    /// <summary>
    /// Converts a <see cref="PhysicalAddress"/> into a standard colon-delimited hex MAC address string.
    /// </summary>
    /// <param name="macAddress">The physical MAC address to format.</param>
    /// <returns>A colon-delimited hex string representation of the MAC address.</returns>
    private static string FormatMacAddress(PhysicalAddress macAddress)
    {
        byte[] bytes = macAddress.GetAddressBytes();
        if (bytes.Length < 6)
        {
            return macAddress.ToString();
        }

        return string.Join(":", bytes.Select(b => b.ToString("X2")));
    }

    /// <summary>
    /// Determines whether the device represents a network router, gateway, or access point.
    /// </summary>
    /// <param name="probe">The target device probe results.</param>
    /// <param name="vendor">The resolved MAC vendor name.</param>
    /// <returns><see langword="true"/> if the device matches router signatures; otherwise, <see langword="false"/>.</returns>
    private bool IsRouter(NetworkDeviceProbeResult probe, string vendor)
    {
        if (_options.Gateway != null && _options.Gateway.Equals(probe.IpAddress))
        {
            return true;
        }

        if (probe.SsdpServerHeader?.Contains("InternetGatewayDevice", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return vendor.Contains("Cisco", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("Netgear", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("TP-Link", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("Linksys", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("Ubiquiti", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("MikroTik", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("ASUSTeK", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the device matches a Nintendo gaming console.
    /// </summary>
    /// <param name="vendor">The resolved MAC vendor name.</param>
    /// <param name="probe">The target device probe results.</param>
    /// <returns><see langword="true"/> if the device is identified as Nintendo; otherwise, <see langword="false"/>.</returns>
    private static bool IsNintendo(string vendor, NetworkDeviceProbeResult probe)
    {
        return vendor.Contains("Nintendo", StringComparison.OrdinalIgnoreCase) ||
               probe.Hostname?.Contains("Nintendo", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Determines whether the device matches a Sony PlayStation console.
    /// </summary>
    /// <param name="vendor">The resolved MAC vendor name.</param>
    /// <param name="probe">The target device probe results.</param>
    /// <returns><see langword="true"/> if the device is identified as PlayStation; otherwise, <see langword="false"/>.</returns>
    private static bool IsPlaystation(string vendor, NetworkDeviceProbeResult probe)
    {
        return vendor.Contains("Sony Interactive", StringComparison.OrdinalIgnoreCase) ||
               probe.SsdpServerHeader?.Contains("PlayStation", StringComparison.OrdinalIgnoreCase) == true ||
               probe.Hostname?.Contains("PlayStation", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Determines whether the device matches a Microsoft Xbox console.
    /// </summary>
    /// <param name="vendor">The resolved MAC vendor name.</param>
    /// <param name="probe">The target device probe results.</param>
    /// <returns><see langword="true"/> if the device is identified as an Xbox; otherwise, <see langword="false"/>.</returns>
    private static bool IsXbox(string vendor, NetworkDeviceProbeResult probe)
    {
        return probe.SsdpServerHeader?.Contains("Xbox", StringComparison.OrdinalIgnoreCase) == true ||
               probe.Hostname?.Contains("Xbox", StringComparison.OrdinalIgnoreCase) == true ||
               (probe.OpenPorts != null && probe.OpenPorts.Contains(3074) && vendor.Contains("Microsoft", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether the device matches a Smart TV or streaming media renderer.
    /// </summary>
    /// <param name="vendor">The resolved MAC vendor name.</param>
    /// <param name="probe">The target device probe results.</param>
    /// <returns><see langword="true"/> if the device is identified as a Smart TV; otherwise, <see langword="false"/>.</returns>
    private static bool IsSmartTv(string vendor, NetworkDeviceProbeResult probe)
    {
        if (vendor.Contains("Roku", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains("LG Electronics", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains("Vizio", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains("TCL", StringComparison.OrdinalIgnoreCase) ||
            vendor.Contains("Hisense", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return probe.SsdpServerHeader?.Contains("MediaRenderer", StringComparison.OrdinalIgnoreCase) == true ||
               probe.SsdpServerHeader?.Contains("SmartTV", StringComparison.OrdinalIgnoreCase) == true ||
               probe.SsdpServerHeader?.Contains("Tizen", StringComparison.OrdinalIgnoreCase) == true ||
               probe.SsdpServerHeader?.Contains("webOS", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Determines whether the device matches a Windows Personal Computer.
    /// </summary>
    /// <param name="probe">The target device probe results.</param>
    /// <returns><see langword="true"/> if the device is identified as a Windows PC; otherwise, <see langword="false"/>.</returns>
    private static bool IsWindowsPc(NetworkDeviceProbeResult probe)
    {
        if (probe.TimeToLive.HasValue && probe.TimeToLive.Value == 128)
        {
            return true;
        }

        if (probe.DhcpVendorClass?.Contains("MSFT", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return probe.OpenPorts != null && (probe.OpenPorts.Contains(135) || probe.OpenPorts.Contains(445));
    }

    /// <summary>
    /// Determines whether the device matches an Android smartphone or tablet.
    /// </summary>
    /// <param name="probe">The target device probe results.</param>
    /// <returns><see langword="true"/> if the device is identified as Android; otherwise, <see langword="false"/>.</returns>
    private static bool IsAndroid(NetworkDeviceProbeResult probe)
    {
        if (probe.DhcpVendorClass?.Contains("android", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return probe.Hostname?.StartsWith("android-", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Determines whether the device matches an Internet of Things (IoT) peripheral or smart home controller.
    /// </summary>
    /// <param name="vendor">The resolved MAC vendor name.</param>
    /// <param name="probe">The target device probe results.</param>
    /// <returns><see langword="true"/> if the device is identified as an IoT device; otherwise, <see langword="false"/>.</returns>
    private static bool IsIoTDevice(string vendor, NetworkDeviceProbeResult probe)
    {
        return vendor.Contains("Espressif", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("Tuya", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("Shelly", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("Sonoff", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("Nest", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("Ring", StringComparison.OrdinalIgnoreCase) ||
               vendor.Contains("TP-Link Corporation Limited", StringComparison.OrdinalIgnoreCase) && (probe.OpenPorts == null || !probe.OpenPorts.Contains(80));
    }

    /// <summary>
    /// Determines whether the device matches a general Linux computer or appliance.
    /// </summary>
    /// <param name="probe">The target device probe results.</param>
    /// <returns><see langword="true"/> if the device is identified as Linux; otherwise, <see langword="false"/>.</returns>
    private static bool IsLinuxDevice(NetworkDeviceProbeResult probe)
    {
        if (probe.TimeToLive.HasValue && probe.TimeToLive.Value == 64 &&
            probe.OpenPorts != null && probe.OpenPorts.Contains(22))
        {
            return true;
        }

        return probe.DhcpVendorClass?.Contains("dhcpcd", StringComparison.OrdinalIgnoreCase) == true;
    }
}
