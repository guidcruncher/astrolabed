using System.Buffers;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Protocol;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp.Services;

/// <summary>
/// Resolves precise system time by querying upstream primary NTP servers and calculating median clock offset.
/// </summary>
/// <param name="optionsMonitor">Monitored NTP options configuration.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class UpstreamTimeResolver(
    IOptionsMonitor<NtpServerOptions> optionsMonitor,
    ILogger<UpstreamTimeResolver> logger) : ITimeResolver, IDisposable
{
    private const int MaxNtpPacketSize = 512;

    private readonly IOptionsMonitor<NtpServerOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly ILogger<UpstreamTimeResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SemaphoreSlim _syncSemaphore = new(1, 1);

    private TimeSpan _clockOffset = TimeSpan.Zero;
    private DateTimeOffset _lastSyncTime = DateTimeOffset.MinValue;
    private bool _disposed;

    /// <inheritdoc />
    public async ValueTask<DateTimeOffset> GetCurrentTimeAsync(CancellationToken cancellationToken = default)
    {
        NtpServerOptions options = _optionsMonitor.CurrentValue;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (_lastSyncTime == DateTimeOffset.MinValue || (now - _lastSyncTime).TotalSeconds >= options.UpstreamSyncIntervalSeconds)
        {
            await RefreshOffsetAsync(cancellationToken).ConfigureAwait(false);
        }

        return DateTimeOffset.UtcNow + _clockOffset;
    }

    private async Task RefreshOffsetAsync(CancellationToken cancellationToken)
    {
        if (!await _syncSemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
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

            IReadOnlyList<string> serverHosts = options.UpstreamServers;
            if (serverHosts is null || serverHosts.Count == 0)
            {
                LogNoUpstreamServersConfigured(_logger);
                return;
            }

            LogSynchronizingWithUpstreamServers(_logger, serverHosts.Count);

            Task<TimeSpan?>[] tasks = new Task<TimeSpan?>[serverHosts.Count];
            for (int i = 0; i < serverHosts.Count; i++)
            {
                tasks[i] = QueryServerOffsetAsync(serverHosts[i], options, cancellationToken);
            }

            TimeSpan?[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

            int validCount = 0;
            Span<TimeSpan> validOffsets = stackalloc TimeSpan[results.Length];

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].HasValue)
                {
                    validOffsets[validCount++] = results[i]!.Value;
                }
            }

            if (validCount == 0)
            {
                LogNoValidResponsesReceived(_logger);
                return;
            }

            Span<TimeSpan> activeSpan = validOffsets[..validCount];
            activeSpan.Sort();

            TimeSpan medianOffset = CalculateMedianOffset(activeSpan);

            _clockOffset = medianOffset;
            _lastSyncTime = DateTimeOffset.UtcNow;

            LogSynchronizationSuccess(_logger, validCount, serverHosts.Count, medianOffset.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            LogSynchronizationFailed(_logger, ex);
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
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(options.UpstreamTimeoutMilliseconds);

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(serverHost, cts.Token).ConfigureAwait(false);
            if (addresses.Length == 0)
            {
                LogHostnameResolutionFailed(_logger, serverHost);
                return null;
            }

            var remoteEndPoint = new IPEndPoint(addresses[0], options.UpstreamPort);

            using var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = options.UpstreamTimeoutMilliseconds,
                SendTimeout = options.UpstreamTimeoutMilliseconds
            };

            var request = new NtpPacket
            {
                Mode = NtpMode.Client,
                VersionNumber = 4,
                TransmitTimestamp = NtpTimestamp.FromDateTimeOffset(DateTimeOffset.UtcNow)
            };

            byte[] sendBuffer = ArrayPool<byte>.Shared.Rent(NtpPacketSerializer.HeaderSize);
            byte[] receiveBuffer = ArrayPool<byte>.Shared.Rent(MaxNtpPacketSize);

            try
            {
                int bytesSerialized = NtpPacketSerializer.Serialize(request, sendBuffer);

                DateTimeOffset t1 = DateTimeOffset.UtcNow;
                await socket.SendToAsync(sendBuffer.AsMemory(0, bytesSerialized), SocketFlags.None, remoteEndPoint, cts.Token).ConfigureAwait(false);

                EndPoint senderEndPoint = new IPEndPoint(remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);

                SocketReceiveFromResult receiveResult = await socket.ReceiveFromAsync(
                    receiveBuffer.AsMemory(0, MaxNtpPacketSize),
                    SocketFlags.None,
                    senderEndPoint,
                    cts.Token).ConfigureAwait(false);

                DateTimeOffset t4 = DateTimeOffset.UtcNow;

                if (receiveResult.ReceivedBytes < NtpPacketSerializer.HeaderSize)
                {
                    LogInvalidUpstreamResponse(_logger, serverHost);
                    return null;
                }

                if (!NtpPacketSerializer.TryDeserialize(receiveBuffer.AsSpan(0, receiveResult.ReceivedBytes), out NtpPacket? response) || response is null)
                {
                    LogInvalidUpstreamResponse(_logger, serverHost);
                    return null;
                }

                DateTimeOffset t2 = response.ReceiveTimestamp.ToDateTimeOffset();
                DateTimeOffset t3 = response.TransmitTimestamp.ToDateTimeOffset();

                // RFC 5905 NTP Clock Offset Calculation: ((t2 - t1) + (t3 - t4)) / 2
                TimeSpan offset = TimeSpan.FromTicks(((t2 - t1).Ticks + (t3 - t4).Ticks) / 2);

                LogTraceServerOffset(_logger, offset.TotalMilliseconds, serverHost);
                return offset;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sendBuffer);
                ArrayPool<byte>.Shared.Return(receiveBuffer);
            }
        }
        catch (Exception ex)
        {
            LogQueryServerFailed(_logger, serverHost, ex);
            return null;
        }
    }

    private static TimeSpan CalculateMedianOffset(ReadOnlySpan<TimeSpan> sortedOffsets)
    {
        int count = sortedOffsets.Length;
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _syncSemaphore.Dispose();
            _disposed = true;
        }
    }

    [LoggerMessage(EventId = 101, Level = LogLevel.Warning, Message = "No upstream NTP servers configured.")]
    private static partial void LogNoUpstreamServersConfigured(ILogger logger);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information, Message = "Synchronizing time with {Count} upstream NTP server(s)...")]
    private static partial void LogSynchronizingWithUpstreamServers(ILogger logger, int count);

    [LoggerMessage(EventId = 103, Level = LogLevel.Warning, Message = "Failed to obtain valid time responses from any upstream NTP servers.")]
    private static partial void LogNoValidResponsesReceived(ILogger logger);

    [LoggerMessage(EventId = 104, Level = LogLevel.Information, Message = "Successfully synchronized with {SuccessfulCount}/{TotalCount} upstream servers. Median clock offset: {Offset} ms")]
    private static partial void LogSynchronizationSuccess(ILogger logger, int successfulCount, int totalCount, double offset);

    [LoggerMessage(EventId = 105, Level = LogLevel.Error, Message = "Failed to synchronize time with upstream NTP servers.")]
    private static partial void LogSynchronizationFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 201, Level = LogLevel.Warning, Message = "Unable to resolve upstream NTP server hostname {Server}")]
    private static partial void LogHostnameResolutionFailed(ILogger logger, string server);

    [LoggerMessage(EventId = 202, Level = LogLevel.Warning, Message = "Received invalid response from upstream NTP server {Server}")]
    private static partial void LogInvalidUpstreamResponse(ILogger logger, string server);

    [LoggerMessage(EventId = 203, Level = LogLevel.Trace, Message = "Received offset {Offset} ms from upstream server {Server}")]
    private static partial void LogTraceServerOffset(ILogger logger, double offset, string server);

    [LoggerMessage(EventId = 204, Level = LogLevel.Warning, Message = "Error querying upstream NTP server {Server}")]
    private static partial void LogQueryServerFailed(ILogger logger, string server, Exception exception);
}
