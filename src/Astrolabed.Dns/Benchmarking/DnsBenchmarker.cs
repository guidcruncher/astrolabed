// File: DnsBenchmarker.cs
namespace Astrolabed.Dns.Benchmarking;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Benchmarking.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// High-performance DNS benchmarking engine utilizing raw UDP sockets.
/// </summary>
public class DnsBenchmarker : IDnsBenchmarker
{
    private readonly DnsBenchmarkOptions _options;
    private readonly ILogger<DnsBenchmarker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsBenchmarker"/> class.
    /// </summary>
    /// <param name="options">The configured benchmarking options.</param>
    /// <param name="logger">The logging infrastructure.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
    public DnsBenchmarker(
        IOptions<DnsBenchmarkOptions> options,
        ILogger<DnsBenchmarker> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<DnsBenchmarkResult> BenchmarkAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing benchmark against all {ServerCount} configured servers.",
            _options.Servers.Count);

        return ExecuteBenchmarkAsync(_options.Servers, cancellationToken);
    }

    /// <inheritdoc />
    public Task<DnsBenchmarkResult?> BenchmarkServerAsync(string serverName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);

        DnsServerConfig? targetServer = _options.Servers.FirstOrDefault(
            s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));

        if (targetServer is null)
        {
            _logger.LogWarning("Benchmark requested for unknown server: '{ServerName}'", serverName);
            return Task.FromResult<DnsBenchmarkResult?>(null);
        }

        _logger.LogInformation("Executing isolated benchmark for server: '{ServerName}'", targetServer.Name);

        return ExecuteBenchmarkAsync([targetServer], cancellationToken)
            .ContinueWith(t => (DnsBenchmarkResult?)t.Result, cancellationToken);
    }

    private async Task<DnsBenchmarkResult> ExecuteBenchmarkAsync(
        IEnumerable<DnsServerConfig> servers,
        CancellationToken cancellationToken)
    {
        List<DnsEndpointMetrics> endpointResults = [];

        foreach (DnsServerConfig server in servers)
        {
            foreach (string ipv4 in server.Ipv4)
            {
                if (IPAddress.TryParse(ipv4, out IPAddress? ip))
                {
                    DnsEndpointMetrics metrics = await MeasureEndpointAsync(server.Name, ip, cancellationToken);
                    endpointResults.Add(metrics);
                }
            }

            foreach (string ipv6 in server.Ipv6)
            {
                if (IPAddress.TryParse(ipv6, out IPAddress? ip))
                {
                    DnsEndpointMetrics metrics = await MeasureEndpointAsync(server.Name, ip, cancellationToken);
                    endpointResults.Add(metrics);
                }
            }
        }

        return new DnsBenchmarkResult(endpointResults.AsReadOnly(), DateTimeOffset.UtcNow);
    }

    private async Task<DnsEndpointMetrics> MeasureEndpointAsync(
        string serverName,
        IPAddress ipAddress,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Benchmarking endpoint {IPAddress} for '{ServerName}'", ipAddress, serverName);

        byte[] queryBuffer = BuildDnsQueryPacket((_options.QueryDomain == "example.com" ? $"{Guid.NewGuid()}.example.com" : _options.QueryDomain));
        IPEndPoint remoteEndpoint = new(ipAddress, 53);

        for (int i = 0; i < _options.WarmupCount; i++)
        {
            await PingDnsServerAsync(remoteEndpoint, queryBuffer, cancellationToken);
        }

        List<double> latenciesMs = [];
        int failedQueries = 0;
        string? lastError = null;

        for (int i = 0; i < _options.Iterations; i++)
        {
            try
            {
                TimeSpan? duration = await PingDnsServerAsync(remoteEndpoint, queryBuffer, cancellationToken);
                if (duration.HasValue)
                {
                    latenciesMs.Add(duration.Value.TotalMilliseconds);
                }
                else
                {
                    failedQueries++;
                }
            }
            catch (Exception ex)
            {
                failedQueries++;
                lastError = ex.Message;
                _logger.LogTrace(ex, "Query attempt failed for {IPAddress}", ipAddress);
            }
        }

        bool isSuccessful = latenciesMs.Count > 0;
        double minLatency = isSuccessful ? latenciesMs.Min() : 0.0;
        double maxLatency = isSuccessful ? latenciesMs.Max() : 0.0;
        double avgLatency = isSuccessful ? latenciesMs.Average() : 0.0;
        double packetLoss = ((double)failedQueries / _options.Iterations) * 100.0;

        return new DnsEndpointMetrics(
            ServerName: serverName,
            IpAddress: ipAddress.ToString(),
            AddressFamily: ipAddress.AddressFamily,
            IsSuccessful: isSuccessful,
            MinimumLatencyMs: Math.Round(minLatency, 2),
            AverageLatencyMs: Math.Round(avgLatency, 2),
            MaximumLatencyMs: Math.Round(maxLatency, 2),
            PacketLossPercentage: Math.Round(packetLoss, 2),
            ErrorMessage: lastError
        );
    }

    private async Task<TimeSpan?> PingDnsServerAsync(
        IPEndPoint remoteEndpoint,
        byte[] queryPacket,
        CancellationToken cancellationToken)
    {
        using Socket socket = new(remoteEndpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            ReceiveTimeout = _options.TimeoutMilliseconds,
            SendTimeout = _options.TimeoutMilliseconds
        };

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.TimeoutMilliseconds);

        try
        {
            long startTimestamp = Stopwatch.GetTimestamp();

            await socket.SendToAsync(queryPacket, SocketFlags.None, remoteEndpoint, cts.Token);

            byte[] receiveBuffer = new byte[512];
            SocketReceiveFromResult result = await socket.ReceiveFromAsync(
                receiveBuffer,
                SocketFlags.None,
                remoteEndpoint,
                cts.Token);

            TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            // A valid DNS response header is at least 12 bytes
            if (result.ReceivedBytes > 12)
            {
                return elapsed;
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private static byte[] BuildDnsQueryPacket(string domain)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        ushort transactionId = (ushort)Random.Shared.Next(1, 65535);

        writer.Write(IPAddress.HostToNetworkOrder((short)transactionId));
        writer.Write((byte)0x01);
        writer.Write((byte)0x00);
        writer.Write(IPAddress.HostToNetworkOrder((short)1));
        writer.Write(IPAddress.HostToNetworkOrder((short)0));
        writer.Write(IPAddress.HostToNetworkOrder((short)0));
        writer.Write(IPAddress.HostToNetworkOrder((short)0));

        string[] labels = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);
        foreach (string label in labels)
        {
            byte[] labelBytes = Encoding.ASCII.GetBytes(label);
            writer.Write((byte)labelBytes.Length);
            writer.Write(labelBytes);
        }
        writer.Write((byte)0);

        writer.Write(IPAddress.HostToNetworkOrder((short)1));
        writer.Write(IPAddress.HostToNetworkOrder((short)1));

        return stream.ToArray();
    }
}
