// File: src/Astrolabed.Dns/Services/DnsUdpListener.cs
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

public sealed class DnsUdpListener : IDnsListener
{
    private readonly IDnsQueryProcessor _queryProcessor;
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;
    private readonly Channel<DnsUdpReceiveResult> _incomingUdpChannel;
    private readonly ILogger<DnsUdpListener> _logger;

    public DnsUdpListener(
        IDnsQueryProcessor queryProcessor,
        IOptionsMonitor<DnsEngineOptions> optionsMonitor,
    ILogger<DnsUdpListener> logger)
    {
        _queryProcessor = queryProcessor;
        _optionsMonitor = optionsMonitor;
        _logger = logger;

        _incomingUdpChannel = Channel.CreateUnbounded<DnsUdpReceiveResult>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            AllowSynchronousContinuations = true
        });
    }

    public async Task ListenAsync(IPAddress address, int port, CancellationToken ct)
    {
        var options = _optionsMonitor.CurrentValue;
        var workerTasks = new Task[options.ProcessingThreads];
        _logger.LogInformation("Starting Udp Listener on {Address}#{Port}", address.ToString(), port.ToString());

        for (int i = 0; i < options.ProcessingThreads; i++)
        {
            workerTasks[i] = Task.Run(() => ProcessUdpPacketQueueAsync(ct), ct);
        }

        var listenTask = Task.Run(() => ListenUdpAsync(address, port, ct), ct);

        _logger.LogInformation("Udp Listener Started on {Address}#{Port}", address.ToString(), port.ToString());

        await Task.WhenAll(listenTask, Task.WhenAll(workerTasks)).ConfigureAwait(false);
    }

    private async Task ListenUdpAsync(IPAddress address, int port, CancellationToken ct)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(address, port));

        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), ct).ConfigureAwait(false);

                var packetCopy = new byte[result.ReceivedBytes];
                Array.Copy(buffer, 0, packetCopy, 0, result.ReceivedBytes);

                _incomingUdpChannel.Writer.TryWrite(new DnsUdpReceiveResult(packetCopy, result.RemoteEndPoint, socket));
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _incomingUdpChannel.Writer.Complete();
        }
    }

    private async Task ProcessUdpPacketQueueAsync(CancellationToken ct)
    {
        var reader = _incomingUdpChannel.Reader;
        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out var item))
            {
                byte[]? responseBytes = await _queryProcessor.ProcessRequestAsync(item.Buffer, item.RemoteEndPoint, ct).ConfigureAwait(false);
                if (responseBytes != null)
                {
                    await item.ServerSocket.SendToAsync(responseBytes, SocketFlags.None, item.RemoteEndPoint, ct).ConfigureAwait(false);
                }
            }
        }
    }
}
