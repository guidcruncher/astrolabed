using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Astrolabed.Dns.ConditionalForwarding;
using Astrolabed.Events;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Core;

public sealed class DnsServer : BackgroundService
{
    private const int SocketBufferSize = 4 * 1024 * 1024;
    private const int BufferSize = 4096;
    private static readonly TimeSpan TcpClientIdleTimeout = TimeSpan.FromSeconds(15);

    private readonly ILogger<DnsServer> _logger;
    private readonly DnsForwarderOptions _options;
    private readonly DnsForwarderService _forwarder;
    private readonly IDnsMetrics _metrics;
    private readonly IConditionalDnsForwarder _conditionalForwarder;

    private Socket? _udpSocket;
    private Socket? _tcpSocket;
    private Channel<PooledUdpPacket>? _channel;

    public DnsServer(
        ILogger<DnsServer> logger,
        IOptions<DnsForwarderOptions> options,
        DnsForwarderService forwarder,
        IDnsMetrics metrics,
        IConditionalDnsForwarder conditionalForwarder)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(forwarder);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(conditionalForwarder);

        _logger = logger;
        _options = options.Value;
        _forwarder = forwarder;
        _metrics = metrics;
        _conditionalForwarder = conditionalForwarder;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IPAddress.TryParse(_options.Listen.Address, out var listenAddress))
        {
            _logger.LogCritical("Invalid IP Address in DNS ListenAddress. Cannot initialise DNS Service");
            return;
        }

        var endpoint = new IPEndPoint(listenAddress, _options.Listen.Port);

        _udpSocket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            ReceiveBufferSize = SocketBufferSize,
            SendBufferSize = SocketBufferSize
        };
        _udpSocket.Bind(endpoint);

        _tcpSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
            ReceiveBufferSize = SocketBufferSize,
            SendBufferSize = SocketBufferSize
        };
        _tcpSocket.Bind(endpoint);
        _tcpSocket.Listen(128);

        _logger.LogInformation(
            "DNS forwarder listening on {Address}:{Port} (UDP & TCP)",
            _options.Listen.Address,
            _options.Listen.Port);

        _channel = Channel.CreateBounded<PooledUdpPacket>(new BoundedChannelOptions(4096)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        });

        int workerCount = Math.Max(1, Environment.ProcessorCount);
        var tasks = new List<Task>(workerCount + 2);

        for (int i = 0; i < workerCount; i++)
        {
            tasks.Add(Task.Run(() => ProcessWorkerQueueAsync(stoppingToken), stoppingToken));
        }

        tasks.Add(Task.Run(() => ListenUdpAsync(endpoint, stoppingToken), stoppingToken));
        tasks.Add(Task.Run(() => ListenTcpAsync(stoppingToken), stoppingToken));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ListenUdpAsync(IPEndPoint endpoint, CancellationToken stoppingToken)
    {
        EndPoint dummyRemoteEp = endpoint.AddressFamily == AddressFamily.InterNetwork
            ? new IPEndPoint(IPAddress.Any, 0)
            : new IPEndPoint(IPAddress.IPv6Any, 0);

        while (!stoppingToken.IsCancellationRequested)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            bool ownershipTransferred = false;

            try
            {
                var result = await _udpSocket!.ReceiveFromAsync(
                    buffer,
                    SocketFlags.None,
                    dummyRemoteEp,
                    stoppingToken).ConfigureAwait(false);

                var packet = new PooledUdpPacket(buffer, result.ReceivedBytes, result.RemoteEndPoint);

                try
                {
                    if (!_channel!.Writer.TryWrite(packet))
                    {
                        await _channel.Writer.WriteAsync(packet, stoppingToken).ConfigureAwait(false);
                    }
                    ownershipTransferred = true;
                }
                catch
                {
                    packet.Dispose();
                    throw;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                if (!ownershipTransferred)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                break;
            }
            catch (Exception ex)
            {
                if (!ownershipTransferred)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                _logger.LogError(ex, "Error receiving UDP DNS packet");
            }
        }

        _channel!.Writer.Complete();
    }

    private async Task ListenTcpAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Socket clientSocket = await _tcpSocket!.AcceptAsync(stoppingToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleTcpConnectionAsync(clientSocket, stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting TCP DNS connection");
            }
        }
    }

    private async Task HandleTcpConnectionAsync(Socket clientSocket, CancellationToken stoppingToken)
    {
        using (clientSocket)
        {
            if (clientSocket.RemoteEndPoint is not IPEndPoint clientEp)
            {
                return;
            }

            byte[] lengthBuffer = ArrayPool<byte>.Shared.Rent(2);
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeoutCts.CancelAfter(TcpClientIdleTimeout);

                    bool success = await TryReadExactAsync(clientSocket, lengthBuffer.AsMemory(0, 2), timeoutCts.Token).ConfigureAwait(false);
                    if (!success)
                    {
                        break;
                    }

                    ushort payloadLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer.AsSpan(0, 2));
                    if (payloadLength < 12)
                    {
                        _logger.LogWarning("Malformed TCP DNS frame length ({Length}) from {Remote}", payloadLength, clientEp);
                        break;
                    }

                    byte[] payloadBuffer = ArrayPool<byte>.Shared.Rent(payloadLength);
                    try
                    {
                        await ReadExactAsync(clientSocket, payloadBuffer.AsMemory(0, payloadLength), timeoutCts.Token).ConfigureAwait(false);
                        await ProcessTcpRequestAsync(clientSocket, clientEp, payloadBuffer, payloadLength, stoppingToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(payloadBuffer);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (SocketException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error servicing TCP DNS connection from {Remote}", clientEp);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(lengthBuffer);
            }
        }
    }

    private async Task ProcessTcpRequestAsync(
        Socket clientSocket,
        IPEndPoint clientEp,
        byte[] requestBuffer,
        int requestLength,
        CancellationToken ct)
    {
        long startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            string? clientName = await ResolveClientHostnameAsync(clientEp.Address, ct).ConfigureAwait(false);
            var parsed = DnsMessage.TryParse(requestBuffer);

            if (parsed is not null)
            {
                _metrics.RecordDnsQuery(new DnsQueryEvent(
                    Timestamp: DateTime.UtcNow,
                    ClientIp: clientEp.Address,
                    ClientName: clientName,
                    QueryName: parsed.QuestionName,
                    QueryType: parsed.QuestionType));
            }

            var requestBytes = GC.AllocateUninitializedArray<byte>(requestLength);
            requestBuffer.AsSpan(0, requestLength).CopyTo(requestBytes);

            var response = await _forwarder.ProcessAsync(
                requestBytes,
                clientEp,
                ct).ConfigureAwait(false);

            if (response is not null)
            {
                ushort responseLength = (ushort)response.Length;
                byte[] sendBuffer = ArrayPool<byte>.Shared.Rent(2 + responseLength);
                try
                {
                    BinaryPrimitives.WriteUInt16BigEndian(sendBuffer.AsSpan(0, 2), responseLength);
                    response.Buffer.AsSpan(0, responseLength).CopyTo(sendBuffer.AsSpan(2));

                    await clientSocket.SendAsync(
                        sendBuffer.AsMemory(0, 2 + responseLength),
                        SocketFlags.None,
                        ct).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(sendBuffer);
                }

                var resp = DnsMessage.TryParse(response.Buffer);

                if (resp is not null)
                {
                    _metrics.RecordDnsResponse(new DnsResponseEvent(
                        Timestamp: DateTime.UtcNow,
                        ClientIp: clientEp.Address,
                        ClientName: clientName,
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
                "Error processing TCP DNS request from {Remote}",
                clientEp);
        }
        finally
        {
            double elapsedSeconds = Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds;
            _metrics.RecordDnsLatency(elapsedSeconds);
        }
    }

    private async Task ProcessWorkerQueueAsync(CancellationToken ct)
    {
        var reader = _channel!.Reader;

        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out var packet))
            {
                using (packet)
                {
                    try
                    {
                        await HandleUdpRequestAsync(packet, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling UDP DNS request in worker");
                    }
                }
            }
        }
    }

    private async Task HandleUdpRequestAsync(PooledUdpPacket packet, CancellationToken ct)
    {
        long startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            var parsed = DnsMessage.TryParse(packet.Buffer);
            IPAddress clientIp = ((IPEndPoint)packet.RemoteEndPoint).Address;
            string? clientName = await ResolveClientHostnameAsync(clientIp, ct).ConfigureAwait(false);

            if (parsed is not null)
            {
                _metrics.RecordDnsQuery(new DnsQueryEvent(
                    Timestamp: DateTime.UtcNow,
                    ClientIp: clientIp,
                    ClientName: clientName,
                    QueryName: parsed.QuestionName,
                    QueryType: parsed.QuestionType));
            }

            var requestBytes = GC.AllocateUninitializedArray<byte>(packet.Length);
            packet.Buffer.AsSpan(0, packet.Length).CopyTo(requestBytes);

            var response = await _forwarder.ProcessAsync(
                requestBytes,
                (IPEndPoint)packet.RemoteEndPoint,
                ct).ConfigureAwait(false);

            if (response is not null)
            {
                await _udpSocket!.SendToAsync(
                    response.Buffer.AsMemory(0, response.Length),
                    SocketFlags.None,
                    packet.RemoteEndPoint,
                    ct).ConfigureAwait(false);

                var resp = DnsMessage.TryParse(response.Buffer);

                if (resp is not null)
                {
                    _metrics.RecordDnsResponse(new DnsResponseEvent(
                        Timestamp: DateTime.UtcNow,
                        ClientIp: clientIp,
                        ClientName: clientName,
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
                "Error processing UDP DNS request from {Remote}",
                packet.RemoteEndPoint);
        }
        finally
        {
            double elapsedSeconds = Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds;
            _metrics.RecordDnsLatency(elapsedSeconds);
        }
    }

    private async Task<string?> ResolveClientHostnameAsync(IPAddress clientIp, CancellationToken ct)
    {
        try
        {
            string ptrQueryName = FormatPtrDomain(clientIp);
            const ushort ptrQueryType = 12; // PTR record type

            if (ConditionalDnsForwarder.IsLocalhost(ptrQueryName))
            {
                return "localhost";
            }

            if (!_conditionalForwarder.ShouldForwardToLocalDhcp(ptrQueryName, ptrQueryType))
            {
                return null;
            }

            byte[] ptrRequestPacket = DnsMessage.CreatePtrQuery(ptrQueryName);
            byte[] responseBuffer = await _conditionalForwarder.ForwardToLocalDhcpAsync(ptrRequestPacket, ct).ConfigureAwait(false);

            if (responseBuffer.Length == 0)
            {
                return null;
            }

            var parsedResponse = DnsMessage.TryParse(responseBuffer);
            return parsedResponse?.AnswerHostName;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to resolve PTR client hostname for {ClientIp}", clientIp);
            return null;
        }
    }

    private static string FormatPtrDomain(IPAddress ip)
    {
        byte[] bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return $"{bytes[3]}.{bytes[2]}.{bytes[1]}.{bytes[0]}.in-addr.arpa";
        }

        var nibbles = new char[64];
        for (int i = 0; i < 16; i++)
        {
            byte b = bytes[15 - i];
            nibbles[i * 4] = GetHexChar(b & 0x0F);
            nibbles[i * 4 + 1] = '.';
            nibbles[i * 4 + 2] = GetHexChar((b >> 4) & 0x0F);
            nibbles[i * 4 + 3] = '.';
        }

        return new string(nibbles) + "ip6.arpa";
    }

    private static char GetHexChar(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _udpSocket?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing UDP listener socket");
        }

        try
        {
            _tcpSocket?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing TCP listener socket");
        }

        return base.StopAsync(cancellationToken);
    }

    private static async ValueTask ReadExactAsync(Socket socket, Memory<byte> target, CancellationToken ct)
    {
        if (!await TryReadExactAsync(socket, target, ct).ConfigureAwait(false))
        {
            throw new SocketException((int)SocketError.ConnectionReset);
        }
    }

    private static async ValueTask<bool> TryReadExactAsync(Socket socket, Memory<byte> target, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < target.Length)
        {
            int read = await socket.ReceiveAsync(target[totalRead..], SocketFlags.None, ct).ConfigureAwait(false);
            if (read == 0)
            {
                return false;
            }
            totalRead += read;
        }
        return true;
    }

    private readonly struct PooledUdpPacket : IDisposable
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

        public void Dispose()
        {
            if (Buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(Buffer);
            }
        }
    }
}
