using System;
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

    private readonly object _stateLock = new();
    private DateTime _referenceUtc = DateTime.UtcNow;
    private TimeSpan _offset = TimeSpan.Zero;
    private int _stratum = 2;

    public UpstreamNtpTimeSource(
        ILogger<UpstreamNtpTimeSource> logger,
        IOptions<NtpServerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _options = options.Value.Upstream;

        _syncTask = Task.Run(() => SyncLoopAsync(_cts.Token));
    }

    public Task<NtpTimeResult> GetTimeAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        TimeSpan offset;
        DateTime refUtc;
        int stratum;

        lock (_stateLock)
        {
            offset = _offset;
            refUtc = _referenceUtc;
            stratum = _stratum;
        }

        var now = DateTime.UtcNow + offset;

        return Task.FromResult(new NtpTimeResult(
            UtcNow: now,
            Offset: offset,
            Stratum: stratum,
            ReferenceUtc: refUtc));
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
                if (ct.IsCancellationRequested)
                {
                    break;
                }

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
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), ct)
                          .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SyncOnceAsync(string server, CancellationToken ct)
    {
        using var udp = new UdpClient();
        udp.Client.ReceiveTimeout = 3000;

        var ipAddresses = await System.Net.Dns.GetHostAddressesAsync(server, ct).ConfigureAwait(false);
        if (ipAddresses.Length == 0)
        {
            _logger.LogWarning("DNS lookup returned no IP addresses for server {Server}", server);
            return;
        }

        var endpoint = new IPEndPoint(ipAddresses[0], 123);
        var request = new byte[48];
        request[0] = 0b_00100011; // LI=0, VN=4, Mode=3 (client)

        var t1 = DateTime.UtcNow;

        await udp.SendAsync(request, request.Length, endpoint).ConfigureAwait(false);

        var response = await udp.ReceiveAsync(ct).ConfigureAwait(false);
        var t4 = DateTime.UtcNow;

        ParseResponse(response.Buffer, t1, t4);
    }

    private void ParseResponse(byte[] buffer, DateTime t1, DateTime t4)
    {
        if (buffer.Length < 48)
        {
            throw new ArgumentException("Upstream NTP response must be at least 48 bytes.");
        }

        var t2 = NtpTimestamp.ReadTimestamp(buffer.AsSpan(32, 8));
        var t3 = NtpTimestamp.ReadTimestamp(buffer.AsSpan(40, 8));

        var offset = ((t2 - t1) + (t3 - t4)) / 2;
        var delay = (t4 - t1) - (t3 - t2);

        lock (_stateLock)
        {
            _offset = offset;
            _referenceUtc = t3;
            _stratum = buffer[1];
        }

        _logger.LogDebug(
            "Upstream NTP sync: offset={Offset} delay={Delay} stratum={Stratum}",
            offset, delay, buffer[1]);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        try
        {
            await _syncTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception encountered while stopping NTP synchronization loop.");
        }

        _cts.Dispose();
    }
}
