// File: src/Astrolabed.Dns/Services/DnsEngine.cs
using System;
using System.Buffers;
using System.Collections.Immutable;
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
    private readonly ILogger<DnsEngine> _logger;
    private readonly Channel<Astrolabed.Dns.Models.UdpReceiveResult> _incomingChannel;

    public DnsEngine(
        IOptionsMonitor<DnsEngineOptions> optionsMonitor,
        IDnsCache cache,
        IDomainFilter domainFilter,
        IHostRecordResolver hostResolver,
        IPtrResolver ptrResolver,
        ILogger<DnsEngine> logger)
    {
        _optionsMonitor = optionsMonitor;
        _cache = cache;
        _domainFilter = domainFilter;
        _hostResolver = hostResolver;
        _ptrResolver = ptrResolver;
        _logger = logger;

        var initialOptions = _optionsMonitor.CurrentValue;
        ThreadPool.SetMinThreads(initialOptions.ProcessingThreads * 2, initialOptions.ProcessingThreads * 2);

        _incomingChannel = Channel.CreateUnbounded<Astrolabed.Dns.Models.UdpReceiveResult>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            AllowSynchronousContinuations = true
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.CurrentValue;
        _logger.LogInformation("Starting  DNS Engine on Port {Port}...", options.Port);

        var workerTasks = new Task[options.ProcessingThreads];
        for (int i = 0; i < options.ProcessingThreads; i++)
        {
            workerTasks[i] = Task.Run(() => ProcessPacketQueueAsync(stoppingToken), stoppingToken);
        }

        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.Bind(new IPEndPoint(IPAddress.Any, options.Port));

        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), stoppingToken).ConfigureAwait(false);

                var packetCopy = new byte[result.ReceivedBytes];
                Array.Copy(buffer, 0, packetCopy, 0, result.ReceivedBytes);

                _incomingChannel.Writer.TryWrite(new Astrolabed.Dns.Models.UdpReceiveResult(packetCopy, result.RemoteEndPoint, socket));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            _incomingChannel.Writer.Complete();
            await Task.WhenAll(workerTasks).ConfigureAwait(false);
        }
    }

    private async Task ProcessPacketQueueAsync(CancellationToken ct)
    {
        var reader = _incomingChannel.Reader;
        while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            while (reader.TryRead(out var item))
            {
                await ProcessSinglePacketAsync(item.Buffer, item.RemoteEndPoint, item.ServerSocket, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessSinglePacketAsync(byte[] rawPacket, EndPoint clientEndpoint, Socket socket, CancellationToken ct)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        if (!DnsWireParser.TryParse(rawPacket, out var request) || request == null)
        {
            return;
        }

        byte[]? responseBytes = null;
        string resolutionSource = "UNKNOWN";

        try
        {
            // 1. Cache Check
            if (_cache.TryGet(request.QuestionName, (ushort)request.QuestionType, out var cachedPayload))
            {
                responseBytes = cachedPayload;
                resolutionSource = "CACHE";
                return;
            }

            // 2. Blocklist / Allowlist Filter Evaluation
            if (!_domainFilter.IsAllowed(request.QuestionName) && _domainFilter.IsBlocked(request.QuestionName, out var reason))
            {
                var ede = new ExtendedDnsError
                {
                    InfoCode = ExtendedDnsErrorCode.Filtered,
                    ExtraText = reason ?? "Blocked by policy filter"
                };

                responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.Refused, ede: ede);
                resolutionSource = "BLOCKED_EDE";
                return;
            }

            // 3. Hosts File Resolution (A / AAAA)
            if ((request.QuestionType == DnsType.A || request.QuestionType == DnsType.AAAA) &&
                _hostResolver.TryResolveHost(request.QuestionName, request.QuestionType, out var matchedIp))
            {
                var record = new DnsResourceRecord
                {
                    Name = request.QuestionName,
                    Type = request.QuestionType,
                    Ttl = 300,
                    ParsedIp = matchedIp
                };

                responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NoError, new[] { record });
                resolutionSource = "HOSTS_FILE";
                return;
            }

            // 4. Reverse PTR Lookup Resolution
            if (request.QuestionType == DnsType.PTR && _ptrResolver.TryResolvePtr(request.QuestionName, out var targetDomain) && targetDomain != null)
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
                return;
            }

            // 5. Upstream Forwarding
            responseBytes = await ExecuteUpstreamQueryAsync(rawPacket, ct).ConfigureAwait(false);
            resolutionSource = "UPSTREAM";

            if (responseBytes != null)
            {
                _cache.Store(request.QuestionName, (ushort)request.QuestionType, responseBytes, TimeSpan.FromMinutes(5));
            }
            else
            {
                var ede = new ExtendedDnsError
                {
                    InfoCode = ExtendedDnsErrorCode.NoReachableAuthority,
                    ExtraText = "Upstream resolvers unreachable"
                };

                responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.ServFail, ede: ede);
                resolutionSource = "UPSTREAM_SERVFAIL_EDE";
            }
        }
        finally
        {
            if (responseBytes != null)
            {
                await socket.SendToAsync(responseBytes, SocketFlags.None, clientEndpoint, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Query [{Domain} | {Type}] Client: {Client} Source: {Source} Elapsed: {Elapsed:F2}ms",
                request.QuestionName, request.QuestionType, clientEndpoint, resolutionSource, (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);
        }
    }

    private async Task<byte[]?> ExecuteUpstreamQueryAsync(byte[] rawRequest, CancellationToken ct)
    {
        var upstreams = _optionsMonitor.CurrentValue.UpstreamResolvers;
        if (upstreams.Count == 0) return null;

        if (!IPAddress.TryParse(upstreams[0], out var upstreamIp)) return null;

        var upstreamEp = new IPEndPoint(upstreamIp, 53);
        using var upstreamSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        upstreamSocket.ReceiveTimeout = 2000;

        try
        {
            await upstreamSocket.SendToAsync(rawRequest, SocketFlags.None, upstreamEp, ct).ConfigureAwait(false);
            var buffer = new byte[4096];
            var result = await upstreamSocket.ReceiveFromAsync(buffer, SocketFlags.None, upstreamEp, ct).ConfigureAwait(false);
            return buffer.AsSpan(0, result.ReceivedBytes).ToArray();
        }
        catch
        {
            return null;
        }
    }
}

