using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Astrolabed.Data.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

public class NetworkScannerService : INetworkScannerService
{
    private readonly NetworkScannerOptions _options;
    private readonly ILogger<NetworkScannerService> _logger;

    public NetworkScannerService(
        IOptions<NetworkScannerOptions> options,
        ILogger<NetworkScannerService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyCollection<DiscoveredLanDevice>> ScanLanAsync(CancellationToken cancellationToken = default)
    {

        var localInterface = GetActiveIPv4Interface();
        if (localInterface is null)
        {
            _logger.LogError("No active non-loopback IPv4 network interface detected.");
            return Array.Empty<DiscoveredLanDevice>();
        }

        var (localIp, subnetMask) = localInterface.Value;
        var subnetAddresses = GetSubnetAddresses(localIp, subnetMask);

        _logger.LogInformation("Warming OS ARP table for {Count} target IPs on {LocalIp}...", subnetAddresses.Count, localIp);

        await WarmArpCacheAsync(subnetAddresses, cancellationToken);

        _logger.LogInformation("Reading OS ARP/Neighbor table on platform {OS}...", RuntimeInformation.OSDescription);

        var arpCache = await GetArpCacheAsync(cancellationToken);
        var results = new List<DiscoveredLanDevice>();

        foreach (var targetIp in subnetAddresses)
        {
            if (arpCache.TryGetValue(targetIp.ToString(), out var macAddress))
            {
                string? hostName = null;
                try
                {
                    var hostEntry = await System.Net.Dns.GetHostEntryAsync(targetIp.ToString(), cancellationToken);
                    hostName = hostEntry.HostName;
                }
                catch (SocketException)
                {
                    // Host responded to ARP but reverse DNS lookup was unresolvable
                }

                var device = new DiscoveredLanDevice(targetIp, macAddress, hostName, DateTimeOffset.UtcNow);
                results.Add(device);

                _logger.LogInformation("Discovered LAN Host: {IP} [{MAC}] ({HostName})", targetIp, macAddress, hostName ?? "Unknown");
            }
        }

        _logger.LogInformation("LAN ARP Scan complete. Discovered {Count} active devices.", results.Count);
        return results.AsReadOnly();
    }

    private async Task WarmArpCacheAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(addresses, parallelOptions, async (ip, ct) =>
        {
            try
            {
                using var udpClient = new UdpClient();
                udpClient.Client.SendTimeout = _options.PingTimeoutMs;
                var dummyData = new byte[] { 0x00 };
                await udpClient.SendAsync(dummyData, dummyData.Length, new IPEndPoint(ip, 33434));
            }
            catch
            {
                // Packet delivery failures are expected; the purpose is forcing an ARP resolution request from the OS kernel
            }
        });

        // Short sleep to allow the OS network stack to process incoming ARP/NDP replies
        await Task.Delay(200, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, string>> GetArpCacheAsync(CancellationToken cancellationToken)
    {
        var arpMap = new ConcurrentDictionary<string, string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await ParseCommandOutputAsync("arp", "-a", line =>
            {
                var match = Regex.Match(line, @"(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s+(?<mac>([0-9a-fA-F]{2}[:-]){5}[0-9a-fA-F]{2})");
                if (match.Success)
                {
                    arpMap[match.Groups["ip"].Value] = match.Groups["mac"].Value.Replace('-', ':').ToUpperInvariant();
                }
            }, cancellationToken);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (File.Exists("/proc/net/arp"))
            {
                var lines = await File.ReadAllLinesAsync("/proc/net/arp", cancellationToken);
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
                    var match = Regex.Match(line, @"(?<ip>\S+)\s+dev\s+\S+\s+lladdr\s+(?<mac>([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2})");
                    if (match.Success)
                    {
                        arpMap[match.Groups["ip"].Value] = match.Groups["mac"].Value.ToUpperInvariant();
                    }
                }, cancellationToken);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            await ParseCommandOutputAsync("arp", "-a -n", line =>
            {
                var match = Regex.Match(line, @"\((?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\)\s+at\s+(?<mac>([0-9a-fA-F]{1,2}:){5}[0-9a-fA-F]{1,2})");
                if (match.Success)
                {
                    var formattedMac = string.Join(":", match.Groups["mac"].Value.Split(':').Select(b => b.PadLeft(2, '0')));
                    arpMap[match.Groups["ip"].Value] = formattedMac.ToUpperInvariant();
                }
            }, cancellationToken);
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

        while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
        {
            lineHandler(line);
        }

        await process.WaitForExitAsync(cancellationToken);
    }

    private static (IPAddress LocalIp, IPAddress SubnetMask)? GetActiveIPv4Interface()
    {
        foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (netInterface.OperationalStatus != OperationalStatus.Up ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            foreach (var unicast in netInterface.GetIPProperties().UnicastAddresses)
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
        var ipBytes = ipAddress.GetAddressBytes();
        var maskBytes = mask.GetAddressBytes();

        var networkBytes = new byte[4];
        var broadcastBytes = new byte[4];

        for (var i = 0; i < 4; i++)
        {
            networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            broadcastBytes[i] = (byte)(networkBytes[i] | ~maskBytes[i]);
        }

        var addresses = new List<IPAddress>();
        var current = (uint)(networkBytes[0] << 24 | networkBytes[1] << 16 | networkBytes[2] << 8 | networkBytes[3]) + 1;
        var end = (uint)(broadcastBytes[0] << 24 | broadcastBytes[1] << 16 | broadcastBytes[2] << 8 | broadcastBytes[3]);

        for (var i = current; i < end; i++)
        {
            addresses.Add(new IPAddress(new byte[]
            {
                (byte)((i >> 24) & 0xFF),
                (byte)((i >> 16) & 0xFF),
                (byte)((i >> 8) & 0xFF),
                (byte)(i & 0xFF)
            }));
        }

        return addresses;
    }
}
