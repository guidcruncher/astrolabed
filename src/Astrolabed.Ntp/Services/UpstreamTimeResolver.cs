using System.Net;
using System.Net.Sockets;

using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Protocol;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp.Services;

public class UpstreamTimeResolver : ITimeResolver, IDisposable
{
    private readonly IOptionsMonitor<NtpServerOptions> _optionsMonitor;
    private readonly ILogger<UpstreamTimeResolver> _logger;
    private readonly SemaphoreSlim _syncSemaphore = new(1, 1);

    private TimeSpan _clockOffset = TimeSpan.Zero;
    private DateTimeOffset _lastSyncTime = DateTimeOffset.MinValue;
    private bool _disposed;

    public UpstreamTimeResolver(
        IOptionsMonitor<NtpServerOptions> optionsMonitor,
        ILogger<UpstreamTimeResolver> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async ValueTask<DateTimeOffset> GetCurrentTimeAsync(CancellationToken cancellationToken = default)
    {
        NtpServerOptions options = _optionsMonitor.CurrentValue;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (_lastSyncTime == DateTimeOffset.MinValue || (now - _lastSyncTime).TotalSeconds >= options.UpstreamSyncIntervalSeconds)
        {
            await RefreshOffsetAsync(cancellationToken);
        }

        return DateTimeOffset.UtcNow + _clockOffset;
    }

    private async Task RefreshOffsetAsync(CancellationToken cancellationToken)
    {
        if (!await _syncSemaphore.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            NtpServerOptions options = _optionsMonitor.CurrentValue;
            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (_lastSyncTime != DateTimeOffset.MinValue && (now - _lastSyncTime).TotalSeconds < options.UpstreamSyncIntervalSeconds)
            {
                return;
            }

            List<string> serverHosts = options.UpstreamServers;
            if (serverHosts == null || serverHosts.Count == 0)
            {
                _logger.LogWarning("No upstream NTP servers configured.");
                return;
            }

            _logger.LogInformation("Synchronizing time with {Count} upstream NTP server(s)...", serverHosts.Count);

            List<Task<TimeSpan?>> tasks = new();
            foreach (string serverHost in serverHosts)
            {
                tasks.Add(QueryServerOffsetAsync(serverHost, options, cancellationToken));
            }

            TimeSpan?[] results = await Task.WhenAll(tasks);
            List<TimeSpan> validOffsets = results
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .OrderBy(r => r.Ticks)
                .ToList();

            if (validOffsets.Count == 0)
            {
                _logger.LogWarning("Failed to obtain valid time responses from any upstream NTP servers.");
                return;
            }

            TimeSpan medianOffset = CalculateMedianOffset(validOffsets);

            _clockOffset = medianOffset;
            _lastSyncTime = DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "Successfully synchronized with {SuccessfulCount}/{TotalCount} upstream servers. Median clock offset: {Offset} ms",
                validOffsets.Count,
                serverHosts.Count,
                medianOffset.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synchronize time with upstream NTP servers.");
        }
        finally
        {
            _syncSemaphore.Release();
        }
    }

    private async Task<TimeSpan?> QueryServerOffsetAsync(
        string serverHost,
        NtpServerOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(options.UpstreamTimeoutMilliseconds);

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(serverHost, cts.Token);
            if (addresses.Length == 0)
            {
                _logger.LogWarning("Unable to resolve upstream NTP server hostname {Server}", serverHost);
                return null;
            }

            IPEndPoint remoteEndPoint = new(addresses[0], options.UpstreamPort);
            using UdpClient client = new();
            client.Client.ReceiveTimeout = options.UpstreamTimeoutMilliseconds;
            client.Client.SendTimeout = options.UpstreamTimeoutMilliseconds;

            NtpPacket request = new()
            {
                Mode = NtpMode.Client,
                VersionNumber = 4,
                TransmitTimestamp = NtpTimestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
            };

            byte[] buffer = new byte[NtpPacketSerializer.HeaderSize];
            NtpPacketSerializer.Serialize(request, buffer);

            DateTimeOffset t1 = DateTimeOffset.UtcNow;
            await client.SendAsync(buffer, buffer.Length, remoteEndPoint);

            UdpReceiveResult responseResult = await client.ReceiveAsync(cts.Token);
            DateTimeOffset t4 = DateTimeOffset.UtcNow;

            if (responseResult.Buffer.Length < NtpPacketSerializer.HeaderSize)
            {
                _logger.LogWarning("Received invalid response from upstream NTP server {Server}", serverHost);
                return null;
            }

            NtpPacket response = NtpPacketSerializer.Deserialize(responseResult.Buffer);

            DateTimeOffset t2 = response.ReceiveTimestamp.ToDateTimeOffset();
            DateTimeOffset t3 = response.TransmitTimestamp.ToDateTimeOffset();

            TimeSpan offset = TimeSpan.FromTicks(((t2 - t1).Ticks + (t3 - t4).Ticks) / 2);

            _logger.LogDebug("Received offset {Offset} ms from upstream server {Server}", offset.TotalMilliseconds, serverHost);
            return offset;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error querying upstream NTP server {Server}", serverHost);
            return null;
        }
    }

    private static TimeSpan CalculateMedianOffset(List<TimeSpan> sortedOffsets)
    {
        int count = sortedOffsets.Count;
        if (count == 1)
        {
            return sortedOffsets[0];
        }

        if (count % 2 == 1)
        {
            return sortedOffsets[count / 2];
        }

        long mid1 = sortedOffsets[(count / 2) - 1].Ticks;
        long mid2 = sortedOffsets[count / 2].Ticks;
        return TimeSpan.FromTicks((mid1 + mid2) / 2);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _syncSemaphore.Dispose();
            _disposed = true;
        }
    }
}
