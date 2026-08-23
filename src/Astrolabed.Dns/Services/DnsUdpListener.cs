using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

/// <summary>
/// High-throughput UDP listener utilizing System.Threading.Channels and memory pooling for low-allocation packet processing.
/// </summary>
/// <param name="queryProcessor">DNS query processing engine.</param>
/// <param name="optionsMonitor">Monitored DNS engine configuration options.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class DnsUdpListener(
    IDnsQueryProcessor queryProcessor,
    IOptionsMonitor<DnsEngineOptions> optionsMonitor,
    ILogger<DnsUdpListener> logger) : IDnsListener
{
    private readonly IDnsQueryProcessor _queryProcessor = queryProcessor ?? throw new ArgumentNullException(nameof(queryProcessor));
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly ILogger<DnsUdpListener> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly Channel<PooledUdpPacket> _incomingUdpChannel = Channel.CreateBounded<PooledUdpPacket>(
        new BoundedChannelOptions(10_000)
        {
            SingleWriter = true,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.DropOldest,
            AllowSynchronousContinuations = true
        });

    /// <inheritdoc />
    public async Task ListenAsync(IPAddress address, int port, CancellationToken ct)
    {
        DnsEngineOptions options = _optionsMonitor.CurrentValue;
        int threadCount = Math.Max(1, options.ProcessingThreads);
        var workerTasks = new Task[threadCount];

        LogStartingUdpListener(_logger, address, port);

        for (int i = 0; i < threadCount; i++)
        {
            workerTasks[i] = ProcessUdpPacketQueueAsync(ct);
        }

        Task listenTask = ListenUdpAsync(address, port, ct);

        LogUdpListenerStarted(_logger, address, port);

        await Task.WhenAll(listenTask, Task.WhenAll(workerTasks)).ConfigureAwait(false);
    }

    private async Task ListenUdpAsync(IPAddress address, int port, CancellationToken ct)
    {
        AddressFamily addressFamily = address.AddressFamily == AddressFamily.InterNetworkV6
            ? AddressFamily.InterNetworkV6
            : AddressFamily.InterNetwork;

        using var socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(address, port));

        EndPoint remoteEndPoint = addressFamily == AddressFamily.InterNetworkV6
            ? new IPEndPoint(IPAddress.IPv6Any, 0)
            : new IPEndPoint(IPAddress.Any, 0);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(4096);
                SocketReceiveFromResult result = await socket
                    .ReceiveFromAsync(owner.Memory, SocketFlags.None, remoteEndPoint, ct)
                    .ConfigureAwait(false);

                var packet = new PooledUdpPacket(owner, result.ReceivedBytes, result.RemoteEndPoint, socket);

                if (!_incomingUdpChannel.Writer.TryWrite(packet))
                {
                    packet.Dispose();
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Expected shutdown signal
        }
        finally
        {
            _incomingUdpChannel.Writer.Complete();
        }
    }

    private async Task ProcessUdpPacketQueueAsync(CancellationToken ct)
    {
        ChannelReader<PooledUdpPacket> reader = _incomingUdpChannel.Reader;
        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out PooledUdpPacket item))
            {
                using (item)
                {
                    byte[]? responseBytes = await _queryProcessor
                        .ProcessRequestAsync(item.Buffer.ToArray(), item.RemoteEndPoint, ct)
                        .ConfigureAwait(false);

                    if (responseBytes is { Length: > 0 })
                    {
                        await item.ServerSocket
                            .SendToAsync(responseBytes, SocketFlags.None, item.RemoteEndPoint, ct)
                            .ConfigureAwait(false);
                    }
                }
            }
        }
    }

    private readonly struct PooledUdpPacket(
        IMemoryOwner<byte> memoryOwner,
        int length,
        EndPoint remoteEndPoint,
        Socket serverSocket) : IDisposable
    {
        private readonly IMemoryOwner<byte> _memoryOwner = memoryOwner;

        public ReadOnlySpan<byte> Buffer => _memoryOwner.Memory.Span[..length];
        public EndPoint RemoteEndPoint { get; } = remoteEndPoint;
        public Socket ServerSocket { get; } = serverSocket;

        public void Dispose() => _memoryOwner.Dispose();
    }

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Information,
        Message = "Starting UDP Listener on {Address}#{Port}")]
    private static partial void LogStartingUdpListener(ILogger logger, IPAddress address, int port);

    [LoggerMessage(
        EventId = 302,
        Level = LogLevel.Information,
        Message = "UDP Listener Started on {Address}#{Port}")]
    private static partial void LogUdpListenerStarted(ILogger logger, IPAddress address, int port);
}
