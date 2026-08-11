using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp;

public sealed class UpstreamNtpTimeSource : INtpTimeSource, IAsyncDisposable
{
    private readonly ILogger<UpstreamNtpTimeSource> _logger;
    private readonly UpstreamNtpOptions _options;

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _syncTask;
    private readonly Task _slewWorkerTask;

    private readonly object _stateLock = new();

    private DateTime _referenceUtc = DateTime.UtcNow;
    private TimeSpan _baseOffset = TimeSpan.Zero;
    private TimeSpan _remainingSlew = TimeSpan.Zero;
    private int _stratum = 16;
    private uint _referenceId = 0x4C4F434C; // "LOCL"
    private byte _leapIndicator = 3;        // 3 = Unsynchronized

    private static readonly TimeSpan StepThreshold = TimeSpan.FromMilliseconds(128);
    private const double MaxSlewRateSecondsPerSecond = 0.0005; // 500 PPM (0.5ms/s)

    public UpstreamNtpTimeSource(
        ILogger<UpstreamNtpTimeSource> logger,
        IOptions<NtpServerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _options = options.Value.Upstream;

        _syncTask = Task.Run(() => SyncLoopAsync(_cts.Token));
        _slewWorkerTask = Task.Run(() => SlewLoopAsync(_cts.Token));
    }

    public Task<NtpTimeResult> GetTimeAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        TimeSpan currentOffset;
        DateTime refUtc;
        int stratum;
        uint refId;
        byte leap;

        lock (_stateLock)
        {
            currentOffset = _baseOffset;
            refUtc = _referenceUtc;
            stratum = _stratum;
            refId = _referenceId;
            leap = _leapIndicator;
        }

        var unadjustedNow = DateTime.UtcNow;

        return Task.FromResult(new NtpTimeResult(
            UtcNow: unadjustedNow,
            Offset: currentOffset,
            Stratum: stratum,
            ReferenceUtc: refUtc,
            ReferenceId: refId,
            LeapIndicator: leap));
    }

    private async Task SyncLoopAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Upstream NTP sync disabled.");
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            foreach (var server in _options.Servers)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    await SyncOnceAsync(server, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upstream NTP sync failed for {Server}", server);
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SyncOnceAsync(string server, CancellationToken ct)
    {
        IPAddress[] ipAddresses;
        try
        {
            ipAddresses = await System.Net.Dns.GetHostAddressesAsync(server, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DNS lookup failed for upstream server {Server}", server);
            return;
        }

        if (ipAddresses.Length == 0)
        {
            _logger.LogWarning("DNS lookup returned no IP addresses for server {Server}", server);
            return;
        }

        Random.Shared.Shuffle(ipAddresses);

        using var udp = new UdpClient();

        foreach (var selectedIp in ipAddresses)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

                var endpoint = new IPEndPoint(selectedIp, 123);
                var request = new byte[48];
                request[0] = 0b_00100011; // LI=0, VN=4, Mode=3 (client)

                var t1 = DateTime.UtcNow;
                NtpTimestamp.WriteTimestamp(request.AsSpan(40, 8), t1);

                await udp.SendAsync(request, request.Length, endpoint).ConfigureAwait(false);

                var response = await udp.ReceiveAsync(timeoutCts.Token).ConfigureAwait(false);
                var t4 = DateTime.UtcNow;

                ParseResponse(response.Buffer, t1, t4, selectedIp);

                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed sync attempt against IP {Ip} for server {Server}", selectedIp, server);
            }
        }

        _logger.LogWarning("All resolved IP addresses failed to respond for upstream server {Server}", server);
    }

    private void ParseResponse(byte[] buffer, DateTime t1, DateTime t4, IPAddress serverIp)
    {
        if (buffer.Length < 48)
        {
            throw new ArgumentException("Upstream NTP response must be at least 48 bytes.");
        }

        var t2 = NtpTimestamp.ReadTimestamp(buffer.AsSpan(32, 8));
        var t3 = NtpTimestamp.ReadTimestamp(buffer.AsSpan(40, 8));

        var calculatedOffset = ((t2 - t1) + (t3 - t4)) / 2;
        var delay = (t4 - t1) - (t3 - t2);

        byte incomingLeap = (byte)((buffer[0] >> 6) & 0x03);
        byte incomingStratum = buffer[1];
        int newStratum = incomingStratum < 15 ? incomingStratum + 1 : 16;
        uint newRefId = NtpReferenceId.FormatReferenceId(serverIp, newStratum);

        lock (_stateLock)
        {
            _referenceUtc = t3;
            _stratum = newStratum;
            _referenceId = newRefId;
            _leapIndicator = incomingLeap;

            var offsetDifference = calculatedOffset - _baseOffset;

            if (offsetDifference.Duration() >= StepThreshold)
            {
                _baseOffset = calculatedOffset;
                _remainingSlew = TimeSpan.Zero;
            }
            else
            {
                _remainingSlew += offsetDifference;
            }
        }

        _logger.LogDebug(
            "Upstream NTP sync: offset={Offset} delay={Delay} stratum={Stratum}",
            calculatedOffset, delay, newStratum);
    }

    private async Task SlewLoopAsync(CancellationToken ct)
    {
        const int targetIntervalMs = 100;
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(targetIntervalMs));

        long lastTimestamp = Stopwatch.GetTimestamp();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(ct).ConfigureAwait(false)) break;
            }
            catch (OperationCanceledException)
            {
                break;
            }

            long currentTimestamp = Stopwatch.GetTimestamp();
            double actualElapsedSeconds = Stopwatch.GetElapsedTime(lastTimestamp, currentTimestamp).TotalSeconds;
            lastTimestamp = currentTimestamp;

            double maxSlewSeconds = MaxSlewRateSecondsPerSecond * actualElapsedSeconds;

            lock (_stateLock)
            {
                if (_remainingSlew == TimeSpan.Zero) continue;

                double remainingSeconds = _remainingSlew.TotalSeconds;
                double adjustmentSeconds;

                if (Math.Abs(remainingSeconds) <= maxSlewSeconds)
                {
                    adjustmentSeconds = remainingSeconds;
                    _remainingSlew = TimeSpan.Zero;
                }
                else
                {
                    adjustmentSeconds = Math.Sign(remainingSeconds) * maxSlewSeconds;
                    _remainingSlew -= TimeSpan.FromSeconds(adjustmentSeconds);
                }

                _baseOffset += TimeSpan.FromSeconds(adjustmentSeconds);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        try
        {
            await Task.WhenAll(_syncTask, _slewWorkerTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception encountered during disposal.");
        }

        _cts.Dispose();
    }
}
