namespace Astrolabed.Core.Network;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

/// <summary>
/// Implements active network probing using ICMP, TCP SYN checks, SSDP M-SEARCH, and mDNS unicast queries.
/// </summary>
public class NetworkDeviceProbeService : INetworkDeviceProbeService
{
    private const int PingTimeoutMilliseconds = 500;
    private const int PortScanTimeoutMilliseconds = 300;
    private const int UdpProbeTimeoutMilliseconds = 800;
    private static readonly int[] TargetPorts = [22, 80, 135, 445, 3074, 5000, 7000, 8008, 8009];

    private readonly ILogger<NetworkDeviceProbeService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkDeviceProbeService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is <see langword="null"/>.</exception>
    public NetworkDeviceProbeService(ILogger<NetworkDeviceProbeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<NetworkDeviceProbeResult> ProbeDeviceAsync(
        IPAddress ipAddress,
        PhysicalAddress macAddress,
        string? hostname = null,
        string? dhcpVendorClass = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ipAddress);
        ArgumentNullException.ThrowIfNull(macAddress);

        _logger.LogInformation("Beginning active probe for IP: {IpAddress}", ipAddress);

        Task<int?> ttlTask = ProbeTimeToLiveAsync(ipAddress, cancellationToken);
        Task<IReadOnlyCollection<int>> portsTask = ScanOpenPortsAsync(ipAddress, cancellationToken);
        Task<string?> ssdpTask = ProbeSsdpServerHeaderAsync(ipAddress, cancellationToken);
        Task<string?> mdnsTask = ProbeMdnsModelStringAsync(ipAddress, cancellationToken);

        await Task.WhenAll(ttlTask, portsTask, ssdpTask, mdnsTask).ConfigureAwait(false);

        int? timeToLive = await ttlTask.ConfigureAwait(false);
        IReadOnlyCollection<int> openPorts = await portsTask.ConfigureAwait(false);
        string? ssdpServerHeader = await ssdpTask.ConfigureAwait(false);
        string? mdnsModelString = await mdnsTask.ConfigureAwait(false);

        return new NetworkDeviceProbeResult(
            MacAddress: macAddress,
            IpAddress: ipAddress,
            Hostname: hostname,
            MdnsModelString: mdnsModelString,
            SsdpServerHeader: ssdpServerHeader,
            DhcpVendorClass: dhcpVendorClass,
            TimeToLive: timeToLive,
            OpenPorts: openPorts);
    }

    /// <summary>
    /// Sends an ICMP Echo request to retrieve the IP packet Time To Live (TTL) value.
    /// </summary>
    private async Task<int?> ProbeTimeToLiveAsync(IPAddress ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            using var ping = new Ping();
            PingReply reply = await ping.SendPingAsync(ipAddress, PingTimeoutMilliseconds).ConfigureAwait(false);

            if (reply.Status == IPStatus.Success && reply.Options != null)
            {
                return reply.Options.Ttl;
            }
        }
        catch (Exception ex) when (ex is PingException or SocketException or OperationCanceledException)
        {
            _logger.LogDebug(ex, "ICMP Ping probe failed for IP: {IpAddress}", ipAddress);
        }

        return null;
    }

    /// <summary>
    /// Performs asynchronous TCP connect attempts against target ports to determine active listening services.
    /// </summary>
    private async Task<IReadOnlyCollection<int>> ScanOpenPortsAsync(IPAddress ipAddress, CancellationToken cancellationToken)
    {
        var openPorts = new List<int>();
        var tasks = TargetPorts.Select(port => CheckPortAsync(ipAddress, port, cancellationToken));

        int[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (int openPort in results)
        {
            if (openPort > 0)
            {
                openPorts.Add(openPort);
            }
        }

        return openPorts.AsReadOnly();
    }

    /// <summary>
    /// Attempts a socket connection to a single TCP port.
    /// </summary>
    private async Task<int> CheckPortAsync(IPAddress ipAddress, int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(PortScanTimeoutMilliseconds);

            await client.ConnectAsync(ipAddress, port, cts.Token).ConfigureAwait(false);
            return port;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Sends a unicast UPnP/SSDP M-SEARCH UDP query to retrieve device Server header response metadata.
    /// </summary>
    private async Task<string?> ProbeSsdpServerHeaderAsync(IPAddress ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new UdpClient();
            client.Client.ReceiveTimeout = UdpProbeTimeoutMilliseconds;

            string mSearchQuery =
                "M-SEARCH * HTTP/1.1\r\n" +
                $"HOST: {ipAddress}:1900\r\n" +
                "MAN: \"ssdp:discover\"\r\n" +
                "MX: 1\r\n" +
                "ST: ssdp:all\r\n\r\n";

            byte[] requestBytes = Encoding.UTF8.GetBytes(mSearchQuery);
            var endpoint = new IPEndPoint(ipAddress, 1900);

            await client.SendAsync(requestBytes, requestBytes.Length, endpoint).ConfigureAwait(false);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(UdpProbeTimeoutMilliseconds);

            UdpReceiveResult receiveResult = await client.ReceiveAsync(cts.Token).ConfigureAwait(false);
            string responseText = Encoding.UTF8.GetString(receiveResult.Buffer);

            using var reader = new StringReader(responseText);
            string? line;
            while ((line = await reader.ReadLineAsync(cts.Token).ConfigureAwait(false)) != null)
            {
                if (line.StartsWith("SERVER:", StringComparison.OrdinalIgnoreCase))
                {
                    return line["SERVER:".Length..].Trim();
                }
            }
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or TimeoutException)
        {
            _logger.LogDebug(ex, "SSDP probe timed out or failed for IP: {IpAddress}", ipAddress);
        }

        return null;
    }

    /// <summary>
    /// Sends a unicast mDNS metadata probe to UDP port 5353 to extract TXT record model identifiers.
    /// </summary>
    private async Task<string?> ProbeMdnsModelStringAsync(IPAddress ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new UdpClient();
            byte[] queryPacket = BuildMdnsQueryPacket();
            var endpoint = new IPEndPoint(ipAddress, 5353);

            await client.SendAsync(queryPacket, queryPacket.Length, endpoint).ConfigureAwait(false);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(UdpProbeTimeoutMilliseconds);

            UdpReceiveResult receiveResult = await client.ReceiveAsync(cts.Token).ConfigureAwait(false);
            string responseData = Encoding.ASCII.GetString(receiveResult.Buffer);

            int modelIdx = responseData.IndexOf("model=", StringComparison.OrdinalIgnoreCase);
            if (modelIdx != -1)
            {
                int endIdx = responseData.IndexOf('\0', modelIdx);
                if (endIdx == -1) endIdx = responseData.Length;
                return responseData[modelIdx..endIdx].Trim();
            }
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or TimeoutException)
        {
            _logger.LogDebug(ex, "mDNS probe timed out or failed for IP: {IpAddress}", ipAddress);
        }

        return null;
    }

    /// <summary>
    /// Constructs a basic mDNS DNS packet querying PTR records for local services.
    /// </summary>
    private static byte[] BuildMdnsQueryPacket()
    {
        return
        [
            0x00, 0x00, // Transaction ID
            0x00, 0x00, // Flags
            0x00, 0x01, // Questions (1)
            0x00, 0x00, // Answer RRs
            0x00, 0x00, // Authority RRs
            0x00, 0x00, // Additional RRs
            // Query Name: _services._dns-sd._udp.local
            0x09, (byte)'_', (byte)'s', (byte)'e', (byte)'r', (byte)'v', (byte)'i', (byte)'c', (byte)'e', (byte)'s',
            0x07, (byte)'_', (byte)'d', (byte)'n', (byte)'s', (byte)'-', (byte)'s', (byte)'d',
            0x04, (byte)'_', (byte)'u', (byte)'d', (byte)'p',
            0x05, (byte)'l', (byte)'o', (byte)'c', (byte)'a', (byte)'l',
            0x00,       // End of string
            0x00, 0x0C, // Type: PTR
            0x00, 0x01  // Class: IN
        ];
    }
}
