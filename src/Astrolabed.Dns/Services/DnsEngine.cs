// File: src/Astrolabed.Dns/Services/DnsEngine.cs
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Astrolabed.Dns.Cache;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;
using Astrolabed.Dns.Resolvers;
using Astrolabed.Dns.Serialization;
using Astrolabed.Dns.Upstream;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

public sealed class DnsEngine : BackgroundService
{
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;
    private readonly IDnsCache _cache;
    private readonly IDomainFilter _domainFilter;
    private readonly IHostRecordResolver _hostResolver;
    private readonly IPtrResolver _ptrResolver;
    private readonly IUpstreamClientFactory _upstreamClientFactory;
    private readonly ILogger<DnsEngine> _logger;
    private readonly Channel<DnsUdpReceiveResult> _incomingUdpChannel;

    public DnsEngine(
        IOptionsMonitor<DnsEngineOptions> optionsMonitor,
        IDnsCache cache,
        IDomainFilter domainFilter,
        IHostRecordResolver hostResolver,
        IPtrResolver ptrResolver,
        IUpstreamClientFactory upstreamClientFactory,
        ILogger<DnsEngine> logger)
    {
        _optionsMonitor = optionsMonitor;
        _cache = cache;
        _domainFilter = domainFilter;
        _hostResolver = hostResolver;
        _ptrResolver = ptrResolver;
        _upstreamClientFactory = upstreamClientFactory;
        _logger = logger;

        var initialOptions = _optionsMonitor.CurrentValue;
        ThreadPool.SetMinThreads(initialOptions.ProcessingThreads * 2, initialOptions.ProcessingThreads * 2);

        _incomingUdpChannel = Channel.CreateUnbounded<DnsUdpReceiveResult>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            AllowSynchronousContinuations = true
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var address = string.IsNullOrEmpty(options.ListenAddress.Address) ? IPAddress.Any : IPAddress.Parse(options.ListenAddress.Address);
        int port = options.ListenAddress.Port;

        _logger.LogInformation("Starting DNS Engine (UDP/TCP) on {Address}#{Port}...", address.ToString(), port);

        var workerTasks = new Task[options.ProcessingThreads];
        for (int i = 0; i < options.ProcessingThreads; i++)
        {
            workerTasks[i] = Task.Run(() => ProcessUdpPacketQueueAsync(stoppingToken), stoppingToken);
        }

        var udpTask = Task.Run(() => ListenUdpAsync(address, port, stoppingToken), stoppingToken);
        var tcpTask = Task.Run(() => ListenTcpAsync(address, port, stoppingToken), stoppingToken);

        await Task.WhenAll(udpTask, tcpTask, Task.WhenAll(workerTasks)).ConfigureAwait(false);
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

    private async Task ListenTcpAsync(IPAddress address, int port, CancellationToken ct)
    {
        var listener = new TcpListener(address, port);
        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                _ = Task.Run(() => HandleTcpConnectionAsync(tcpClient, ct), ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleTcpConnectionAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            var lengthBuffer = new byte[2];
            var remoteEndPoint = client.Client?.RemoteEndPoint ?? new IPEndPoint(IPAddress.Loopback, 0);

            while (!ct.IsCancellationRequested && client.Connected)
            {
                int bytesRead = await ReadExactAsync(stream, lengthBuffer, 0, 2, ct).ConfigureAwait(false);
                if (bytesRead < 2) break;

                ushort packetLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBuffer);
                if (packetLength == 0) continue;

                var packetBuffer = ArrayPool<byte>.Shared.Rent(packetLength);
                try
                {
                    int packetBytesRead = await ReadExactAsync(stream, packetBuffer, 0, packetLength, ct).ConfigureAwait(false);
                    if (packetBytesRead < packetLength) break;

                    byte[]? response = await ProcessRequestAsync(packetBuffer.AsSpan(0, packetLength).ToArray(), remoteEndPoint, ct).ConfigureAwait(false);

                    if (response != null)
                    {
                        var responseLengthBuffer = new byte[2];
                        BinaryPrimitives.WriteUInt16BigEndian(responseLengthBuffer, (ushort)response.Length);

                        await stream.WriteAsync(responseLengthBuffer, ct).ConfigureAwait(false);
                        await stream.WriteAsync(response, ct).ConfigureAwait(false);
                        await stream.FlushAsync(ct).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(packetBuffer);
                }
            }
        }
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), ct).ConfigureAwait(false);
            if (read == 0) break;
            totalRead += read;
        }
        return totalRead;
    }

    private async Task ProcessUdpPacketQueueAsync(CancellationToken ct)
    {
        var reader = _incomingUdpChannel.Reader;
        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out var item))
            {
                byte[]? responseBytes = await ProcessRequestAsync(item.Buffer, item.RemoteEndPoint, ct).ConfigureAwait(false);
                if (responseBytes != null)
                {
                    await item.ServerSocket.SendToAsync(responseBytes, SocketFlags.None, item.RemoteEndPoint, ct).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task<byte[]?> ProcessRequestAsync(byte[] rawPacket, EndPoint clientEndpoint, CancellationToken ct)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        if (!DnsWireParser.TryParse(rawPacket, out var request) || request == null)
        {
            return null;
        }

        byte[]? responseBytes = null;
        string resolutionSource = "UNKNOWN";

        try
        {
            // 1. Cache Check
            if (_cache.TryGet(request.QuestionName, (ushort)request.QuestionType, out var cachedPayload))
            {
                responseBytes = (byte[])cachedPayload.Clone();
                BinaryPrimitives.WriteUInt16BigEndian(responseBytes.AsSpan(0, 2), request.TransactionId);

                resolutionSource = "CACHE";
                return responseBytes;
            }

            // 2. Blocklist / Allowlist Filter Evaluation
            if (!_domainFilter.IsAllowed(request.QuestionName) && _domainFilter.IsBlocked(request.QuestionName, out var reason))
            {
                var filterEde = new ExtendedDnsError
                {
                    InfoCode = ExtendedDnsErrorCode.Filtered,
                    ExtraText = reason ?? "Blocked by policy filter"
                };

                responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.Refused, ede: filterEde);
                resolutionSource = "BLOCKED_EDE";
                return responseBytes;
            }

            // 3. Hosts File Resolution (A / AAAA)
            if ((request.QuestionType == DnsType.A || request.QuestionType == DnsType.AAAA) &&
                _hostResolver.TryResolveHost(request.QuestionName, request.QuestionType, out var matchedIp))
            {
                var record = new DnsResourceRecord
                {
                    Name = request.QuestionName,
                    Type = request.QuestionType,
                    Class = 1, // IN (Internet)
                    Ttl = 300,
                    ParsedIp = matchedIp
                };

                responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NoError, new[] { record });
                resolutionSource = "HOSTS_FILE";
                return responseBytes;
            }

            // 4. Reverse PTR Lookup Resolution
            if (request.QuestionType == DnsType.PTR)
            {
                // 4a. Static Overrides Match
                if (_ptrResolver.TryResolvePtr(request.QuestionName, out var targetDomain) && targetDomain != null)
                {
                    var ptrBuffer = new byte[256];
                    int ptrOffset = 0;
                    DnsWireBuilder.EncodeDomainName(ptrBuffer, ref ptrOffset, targetDomain);

                    var record = new DnsResourceRecord
                    {
                        Name = request.QuestionName,
                        Type = DnsType.PTR,
                        Ttl = 300,
                        Data = ptrBuffer.AsSpan(0, ptrOffset).ToArray()
                    };

                    responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NoError, new[] { record });
                    resolutionSource = "LOCAL_PTR";
                    return responseBytes;
                }

                // 4b. Conditional PTR Subnet Forwarding
                if (_ptrResolver is PtrResolver concreteResolver &&
                    concreteResolver.TryGetConditionalForwarder(request.QuestionName, out var targetResolverIp) &&
                    targetResolverIp != null)
                {
                    var upstreamMessage = await _upstreamClientFactory.ExecuteQueryAsync(targetResolverIp.ToString(), rawPacket, ct).ConfigureAwait(false);

                    if (upstreamMessage != null)
                    {
                        upstreamMessage.TransactionId = request.TransactionId;
                        responseBytes = DnsWireBuilder.BuildResponse(upstreamMessage, upstreamMessage.ResponseCode, upstreamMessage.Answers);
                        resolutionSource = "CONDITIONAL_PTR_UPSTREAM";
                        _cache.Store(request.QuestionName, (ushort)request.QuestionType, responseBytes, TimeSpan.FromMinutes(5));
                        return responseBytes;
                    }
                }
            }

            // 5. Default Upstream Forwarding
            var upstreams = _optionsMonitor.CurrentValue.UpstreamResolvers;
            if (upstreams != null && upstreams.Count > 0)
            {
                foreach (var upstream in upstreams)
                {
                    try
                    {
                        var upstreamMessage = await _upstreamClientFactory.ExecuteQueryAsync(upstream, rawPacket, ct).ConfigureAwait(false);

                        if (upstreamMessage != null)
                        {
                            upstreamMessage.TransactionId = request.TransactionId;
                            responseBytes = DnsWireBuilder.BuildResponse(upstreamMessage, upstreamMessage.ResponseCode, upstreamMessage.Answers);
                            resolutionSource = "UPSTREAM";
                            _cache.Store(request.QuestionName, (ushort)request.QuestionType, responseBytes, TimeSpan.FromMinutes(5));
                            return responseBytes;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to resolve query via upstream {Upstream}", upstream);
                    }
                }
            }

            // Fallback: If no hosts match and upstream fails/unreachable, return ServFail or NXDomain instead of dropping packet
            var servfailEde = new ExtendedDnsError
            {
                InfoCode = ExtendedDnsErrorCode.NoReachableAuthority,
                ExtraText = "No host entry match and upstream resolvers unreachable"
            };

            responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.ServFail, ede: servfailEde);
            resolutionSource = "FALLBACK_SERVFAIL";
            return responseBytes;
        }
        finally
        {
            _logger.LogInformation("Query [{Domain} | {Type}] Client: {Client} Source: {Source} Elapsed: {Elapsed:F2}ms",
                request.QuestionName, request.QuestionType, clientEndpoint, resolutionSource, (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);
        }
    }
}
