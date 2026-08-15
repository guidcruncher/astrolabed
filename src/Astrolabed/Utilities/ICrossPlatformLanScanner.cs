using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Utilities;

public record DiscoveredLanDevice(IPAddress IpAddress, string MacAddress, string? HostName);

public class CrossPlatformScannerOptions
{
    public const string SectionName = "NetworkScanner";

    public int MaxDegreeOfParallelism { get; set; } = 100;
    public int PingTimeoutMs { get; set; } = 200;
}

public interface ICrossPlatformLanScannerService
{
    Task<IReadOnlyCollection<DiscoveredLanDevice>> ScanLanAsync(CancellationToken cancellationToken = default);
}

