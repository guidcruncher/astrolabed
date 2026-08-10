using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Astrolabed.Events;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Core;

public sealed class DnsServer : BackgroundService
{
    private const int SocketBufferSize = 4 * 1024 * 1024; // 4 MB socket buffer for high throughput
    private const int BufferSize = 4096; // Standard max DNS over UDP size

    private readonly ILogger<DnsServer> _logger;
    private readonly DnsForwarderOptions _options;
    private readonly DnsForwarderService _forwarder;
    private readonly IDnsMetrics _metrics;

    private Socket? _socket;
    private Channel<PooledUdpPacket>? _channel;

    public DnsServer(
        ILogger<DnsServer> logger,
        DnsForwarderOptions options,
        DnsForwarderService forwarder,
        IDnsMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(forwarder);
        ArgumentNullException.ThrowIfNull(metrics);

        _logger = logger;
        _options = options;
        _forwarder = forwarder;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IPAddress.TryParse(_options.Listen.Address, out var listenAddress))
        {
            _logger.LogCritical("Invalid IP Address in DNS ListenAddress. Cannot initialise DNS Service");
            return;
        }

        var endpoint = new IPEndPoint(listenAddress, _options.Listen.Port);

        _socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            ReceiveBufferSize = SocketBufferSize,
            SendBufferSize = SocketBufferSize
        };

        _socket.Bind(endpoint);

        _logger.LogInformation(
            "DNS forwarder listening on {Address}:{Port}",
            _options.Listen.Address,
            _options.Listen.Port);

        _channel = Channel.CreateBounded<PooledUdpPacket>(new BoundedChannelOptions(4096)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        });

        int workerCount = Math.Max(1, Environment.ProcessorCount);
        var workers = new List<Task>(workerCount);

        for (int i = 0; i < workerCount; i++)
        {
            workers.Add(Task.Run(() => ProcessWorkerQueueAsync(stoppingToken), stoppingToken));
        }

        EndPoint dummyRemoteEp = endpoint.AddressFamily == AddressFamily.InterNetwork
            ? new IPEndPoint(IPAddress.Any, 0)
            : new IPEndPoint(IPAddress.IPv6Any, 0);

        while (!stoppingToken.IsCancellationRequested)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                var result = await _socket.ReceiveFromAsync(
                    buffer,
                    SocketFlags.None,
                    dummyRemoteEp,
                    stoppingToken).ConfigureAwait(false);

                var packet = new PooledUdpPacket(buffer, result.ReceivedBytes, result.RemoteEndPoint);

                if (!_channel.Writer.TryWrite(packet))
                {
                    await _channel.Writer.WriteAsync(packet, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                break;
            }
            catch (Exception ex)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                _logger.LogError(ex, "Error receiving DNS packet");
            }
        }

        _channel.Writer.Complete();
        await Task.WhenAll(workers).ConfigureAwait(false);
    }

    private async Task ProcessWorkerQueueAsync(CancellationToken ct)
    {
        var reader = _channel!.Reader;

        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out var packet))
            {
                try
                {
                    await HandleRequestAsync(packet, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling DNS request in worker");
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(packet.Buffer);
                }
            }
        }
    }

    private async Task HandleRequestAsync(PooledUdpPacket packet, CancellationToken ct)
    {
        long startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            var packetSpan = packet.Buffer.AsSpan(0, packet.Length);
            var parsed = DnsMessage.TryParse(packetSpan);

            IPAddress clientIp = ((IPEndPoint)packet.RemoteEndPoint).Address;

            if (parsed is not null)
            {
                _metrics.RecordDnsQuery(new DnsQueryEvent(
                    Timestamp: DateTime.UtcNow,
                    ClientIp: clientIp,
                    ClientName: null,
                    QueryName: parsed.QuestionName,
                    QueryType: parsed.QuestionType));
            }

            var requestBytes = GC.AllocateUninitializedArray<byte>(packet.Length);
            packetSpan.CopyTo(requestBytes);

            var response = await _forwarder.ProcessAsync(
                requestBytes,
                (IPEndPoint)packet.RemoteEndPoint,
                ct).ConfigureAwait(false);

            if (response is not null)
            {
                await _socket!.SendToAsync(
                    response.Buffer.AsMemory(0, response.Length),
                    SocketFlags.None,
                    packet.RemoteEndPoint,
                    ct).ConfigureAwait(false);

                var respSpan = response.Buffer.AsSpan(0, response.Length);
                var resp = DnsMessage.TryParse(respSpan);

                if (resp is not null)
                {
                    _metrics.RecordDnsResponse(new DnsResponseEvent(
                        Timestamp: DateTime.UtcNow,
                        ClientIp: clientIp,
                        ClientName: null,
                        QueryName: resp.QuestionName,
                        QueryType: resp.QuestionType,
                        Status: resp.ResponseCode.ToString(),
                        ResponseIp: resp.AnswerAddress));
                }

                response.Return();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing DNS request from {Remote}",
                packet.RemoteEndPoint);
        }
        finally
        {
            double elapsedSeconds = Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds;
            _metrics.RecordDnsLatency(elapsedSeconds);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _socket?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing UDP listener socket");
        }

        return base.StopAsync(cancellationToken);
    }

    private readonly struct PooledUdpPacket
    {
        public byte[] Buffer { get; }
        public int Length { get; }
        public EndPoint RemoteEndPoint { get; }

        public PooledUdpPacket(byte[] buffer, int length, EndPoint remoteEndPoint)
        {
            Buffer = buffer;
            Length = length;
            RemoteEndPoint = remoteEndPoint;
        }
    }
}
