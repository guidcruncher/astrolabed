using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Astrolabed.Core.Network;
using Astrolabed.Core.Options;
using Astrolabed.Data.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Scans the local network segment by warming OS ARP/Neighbor caches via UDP probes and parsing platform ARP tables.
/// </summary>
/// <param name="options">Configuration options controlling scanning parameters.</param>
/// <param name="macVendor">The Mac Address Vendor database lookup service.</param>
/// <param name="deviceClassifier">Service used to classify network device types from probe signatures.</param>
/// <param name="probeService">Service used to actively probe hosts for network telemetry.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class NetworkScannerService(
    IOptions<NetworkScannerOptions> options,
    IMacVendorLookupService macVendor,
    INetworkDeviceClassifier deviceClassifier,
    INetworkDeviceProbeService probeService,
    ILogger<NetworkScannerService> logger) : INetworkScannerService
{
    private readonly NetworkScannerOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<NetworkScannerService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMacVendorLookupService _macVendor = macVendor ?? throw new ArgumentNullException(nameof(macVendor));
    private readonly INetworkDeviceClassifier _deviceClassifier = deviceClassifier ?? throw new ArgumentNullException(nameof(deviceClassifier));
    private readonly INetworkDeviceProbeService _probeService = probeService ?? throw new ArgumentNullException(nameof(probeService));

    [GeneratedRegex(@"(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s+(?<mac>([0-9a-fA-F]{2}[:-]){5}[0-9a-fA-F]{2})")]
    private static partial Regex WindowsArpRegex();

    [GeneratedRegex(@"(?<ip>\S+)\s+dev\s+\S+\s+lladdr\s+(?<mac>([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2})")]
    private static partial Regex LinuxNeighRegex();

    [GeneratedRegex(@"\((?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\)\s+at\s+(?<mac>([0-9a-fA-F]{1,2}:){5}[0-9a-fA-F]{1,2})")]
    private static partial Regex MacOsArpRegex();

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<DiscoveredLanDevice>> ScanLanAsync(CancellationToken cancellationToken = default)
    {
        (IPAddress localIp, IPAddress subnetMask)? localInterface = GetActiveIPv4Interface();
        if (localInterface is null)
        {
            LogNoActiveIPv4Interface(_logger);
            return Array.Empty<DiscoveredLanDevice>();
        }

        (IPAddress localIp, IPAddress subnetMask) = localInterface.Value;
        List<IPAddress> subnetAddresses = GetSubnetAddresses(localIp, subnetMask);

        LogWarmingArpCache(_logger, subnetAddresses.Count, localIp);

        await WarmArpCacheAsync(subnetAddresses, cancellationToken).ConfigureAwait(false);

        LogReadingArpTable(_logger, RuntimeInformation.OSDescription);

        IReadOnlyDictionary<string, string> arpCache = await GetArpCacheAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<DiscoveredLanDevice>();

        foreach (IPAddress targetIp in subnetAddresses)
        {
            if (arpCache.TryGetValue(targetIp.ToString(), out string? macAddress))
            {
                string? hostName = null;
                try
                {
                    IPHostEntry hostEntry = await System.Net.Dns.GetHostEntryAsync(targetIp.ToString(), cancellationToken).ConfigureAwait(false);
                    hostName = hostEntry.HostName;
                }
                catch (SocketException)
                {
                    // Host responded to ARP but reverse DNS lookup was unresolvable
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;
                string vendor = "";

                if (MacAddressFormatter.IsRandomizedOui(macAddress))
                {
                    vendor = "<<randomized>>";
                }
                else
                {
                    MacVendorInfo? vendorInfo = _macVendor.FindVendor(macAddress);
                    if (vendorInfo != null)
                    {
                        vendor = vendorInfo.VendorName;
                    }
                }

                PhysicalAddress physicalMac = PhysicalAddress.Parse(macAddress.Replace(':', '-'));

                NetworkDeviceProbeResult probeResult = await _probeService.ProbeDeviceAsync(
                    targetIp,
                    physicalMac,
                    hostName,
                    dhcpVendorClass: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                DeviceType determinedType = _deviceClassifier.ClassifyDevice(probeResult);
                string deviceType = determinedType.ToString();

                var device = new DiscoveredLanDevice(targetIp, macAddress, hostName, now, now, vendor, deviceType);
                results.Add(device);

                LogDiscoveredHost(_logger, targetIp, macAddress, hostName ?? "Unknown");
            }
        }

        LogScanCompleted(_logger, results.Count);
        return results.AsReadOnly();
    }

    private async Task WarmArpCacheAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        byte[] dummyData = [0x00];

        await Parallel.ForEachAsync(addresses, parallelOptions, async (ip, ct) =>
        {
            try
            {
                using var udpClient = new UdpClient();
                udpClient.Client.SendTimeout = _options.PingTimeoutMs;
                await udpClient.SendAsync(dummyData, dummyData.Length, new IPEndPoint(ip, 33434)).ConfigureAwait(false);
            }
            catch
            {
                // Packet delivery failures are expected; the purpose is forcing an ARP resolution request from the OS kernel
            }
        }).ConfigureAwait(false);

        // Allow kernel socket stacks time to process inbound ARP/NDP replies
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<string, string>> GetArpCacheAsync(CancellationToken cancellationToken)
    {
        var arpMap = new ConcurrentDictionary<string, string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await ParseCommandOutputAsync("arp", "-a", line =>
            {
                Match match = WindowsArpRegex().Match(line);
                if (match.Success)
                {
                    arpMap[match.Groups["ip"].Value] = match.Groups["mac"].Value.Replace('-', ':').ToUpperInvariant();
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (File.Exists("/proc/net/arp"))
            {
                string[] lines = await File.ReadAllLinesAsync("/proc/net/arp", cancellationToken).ConfigureAwait(false);
                foreach (string line in lines.Skip(1))
                {
                    string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4 && parts[3] != "00:00:00:00:00:00")
                    {
                        arpMap[parts[0]] = parts[3].ToUpperInvariant();
                    }
                }
            }
            else
            {
                await ParseCommandOutputAsync("ip", "neigh", line =>
                {
                    Match match = LinuxNeighRegex().Match(line);
                    if (match.Success)
                    {
                        arpMap[match.Groups["ip"].Value] = match.Groups["mac"].Value.ToUpperInvariant();
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            await ParseCommandOutputAsync("arp", "-a -n", line =>
            {
                Match match = MacOsArpRegex().Match(line);
                if (match.Success)
                {
                    string formattedMac = string.Join(":", match.Groups["mac"].Value.Split(':').Select(b => b.PadLeft(2, '0')));
                    arpMap[match.Groups["ip"].Value] = formattedMac.ToUpperInvariant();
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        return arpMap;
    }

    private static async Task ParseCommandOutputAsync(string fileName, string arguments, Action<string> lineHandler, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null) return;

        while (await process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            lineHandler(line);
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static (IPAddress LocalIp, IPAddress SubnetMask)? GetActiveIPv4Interface()
    {
        foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (netInterface.OperationalStatus != OperationalStatus.Up ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            foreach (UnicastIPAddressInformation unicast in netInterface.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && unicast.IPv4Mask is not null)
                {
                    return (unicast.Address, unicast.IPv4Mask);
                }
            }
        }

        return null;
    }

    private static List<IPAddress> GetSubnetAddresses(IPAddress ipAddress, IPAddress mask)
    {
        uint ip = BinaryPrimitives.ReadUInt32BigEndian(ipAddress.GetAddressBytes());
        uint maskBits = BinaryPrimitives.ReadUInt32BigEndian(mask.GetAddressBytes());

        uint network = ip & maskBits;
        uint broadcast = network | ~maskBits;

        var addresses = new List<IPAddress>((int)Math.Max(0, (long)broadcast - network - 1));
        Span<byte> buffer = stackalloc byte[4];

        for (uint current = network + 1; current < broadcast; current++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, current);
            addresses.Add(new IPAddress(buffer));
        }

        return addresses;
    }

    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Error,
        Message = "No active non-loopback IPv4 network interface detected.")]
    private static partial void LogNoActiveIPv4Interface(ILogger logger);

    [LoggerMessage(
        EventId = 502,
        Level = LogLevel.Information,
        Message = "Warming OS ARP table for {Count} target IPs on {LocalIp}...")]
    private static partial void LogWarmingArpCache(ILogger logger, int count, IPAddress localIp);

    [LoggerMessage(
        EventId = 503,
        Level = LogLevel.Information,
        Message = "Reading OS ARP/Neighbor table on platform {OS}...")]
    private static partial void LogReadingArpTable(ILogger logger, string os);

    [LoggerMessage(
        EventId = 504,
        Level = LogLevel.Information,
        Message = "Discovered LAN Host: {IP} [{MAC}] ({HostName})")]
    private static partial void LogDiscoveredHost(ILogger logger, IPAddress ip, string mac, string hostName);

    [LoggerMessage(
        EventId = 505,
        Level = LogLevel.Information,
        Message = "LAN ARP Scan complete. Discovered {Count} active devices.")]
    private static partial void LogScanCompleted(ILogger logger, int count);
}
