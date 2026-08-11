using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Ntp;

public sealed class SlewedUpstreamNtpTimeSource : INtpTimeSource, IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _slewWorkerTask;

    private readonly object _stateLock = new();

    // NTP State
    private DateTime _referenceUtc = DateTime.UtcNow;
    private TimeSpan _baseOffset = TimeSpan.Zero;
    private TimeSpan _remainingSlew = TimeSpan.Zero;
    private int _stratum = 2;
    private uint _referenceId = 0x4C4F434C; // "LOCL"

    // Configuration thresholds
    private static readonly TimeSpan StepThreshold = TimeSpan.FromMilliseconds(128);
    private const double MaxSlewRateSecondsPerSecond = 0.0005; // 500 PPM (0.5ms per second)

    public SlewedUpstreamNtpTimeSource()
    {
        _slewWorkerTask = Task.Run(() => SlewLoopAsync(_cts.Token));
    }

    public Task<NtpTimeResult> GetTimeAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        TimeSpan currentOffset;
        DateTime refUtc;
        int stratum;

        lock (_stateLock)
        {
            currentOffset = _baseOffset;
            refUtc = _referenceUtc;
            stratum = _stratum;
        }

        var now = DateTime.UtcNow + currentOffset;

        return Task.FromResult(new NtpTimeResult(
            UtcNow: now,
            Offset: currentOffset,
            Stratum: stratum,
            ReferenceUtc: refUtc));
    }

    public uint GetCurrentReferenceId()
    {
        lock (_stateLock)
        {
            return _referenceId;
        }
    }

    public void ProcessResponse(byte[] buffer, DateTime t1, DateTime t4, IPAddress serverIp)
    {
        if (buffer.Length < 48)
        {
            throw new ArgumentException("Buffer too short for NTP packet.", nameof(buffer));
        }

        var t2 = NtpTimestamp.ReadTimestamp(buffer.AsSpan(32, 8));
        var t3 = NtpTimestamp.ReadTimestamp(buffer.AsSpan(40, 8));

        var calculatedOffset = ((t2 - t1) + (t3 - t4)) / 2;
        byte incomingStratum = buffer[1];
        int newStratum = incomingStratum < 15 ? incomingStratum + 1 : 16;

        uint newRefId = NtpReferenceId.FormatReferenceId(serverIp, newStratum);

        lock (_stateLock)
        {
            _referenceUtc = t3;
            _stratum = newStratum;
            _referenceId = newRefId;

            var offsetDifference = calculatedOffset - _baseOffset;

            // Step adjustment for large jumps
            if (offsetDifference.Duration() >= StepThreshold)
            {
                _baseOffset = calculatedOffset;
                _remainingSlew = TimeSpan.Zero;
            }
            else
            {
                // Queue small adjustments for slewing
                _remainingSlew += offsetDifference;
            }
        }
    }

    private async Task SlewLoopAsync(CancellationToken ct)
    {
        const int intervalMs = 100;
        double intervalSeconds = intervalMs / 1000.0;
        double maxSlewPerStepSeconds = MaxSlewRateSecondsPerSecond * intervalSeconds;

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));

        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            lock (_stateLock)
            {
                if (_remainingSlew == TimeSpan.Zero)
                {
                    continue;
                }

                double remainingSeconds = _remainingSlew.TotalSeconds;
                double adjustmentSeconds;

                if (Math.Abs(remainingSeconds) <= maxSlewPerStepSeconds)
                {
                    adjustmentSeconds = remainingSeconds;
                    _remainingSlew = TimeSpan.Zero;
                }
                else
                {
                    adjustmentSeconds = Math.Sign(remainingSeconds) * maxSlewPerStepSeconds;
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
            await _slewWorkerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _cts.Dispose();
    }
}
