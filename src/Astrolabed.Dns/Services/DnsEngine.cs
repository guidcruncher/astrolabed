// File: src/Astrolabed.Dns/Services/DnsEngine.cs
using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Astrolabed.Dns.Cache;
using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;
using Astrolabed.Dns.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

public sealed class DnsEngine : BackgroundService
{
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;
    private readonly ILockFreeDnsCache _cache;
    private readonly ILogger<OptimizedDnsEngine> _logger;
    private readonly Channel<UdpReceiveResult> _incomingChannel;
    private readonly IDisposable? _optionsChangeToken;

    private EngineStateSnapshot _snapshot;

    public OptimizedDnsEngine(
        IOptionsMonitor<DnsEngineOptions> optionsMonitor,
        ILockFreeDnsCache cache,
        ILogger<OptimizedDnsEngine> logger)
    {
        _optionsMonitor = optionsMonitor;
        _cache = cache;
        _logger = logger;

        var initialOptions = _optionsMonitor.CurrentValue;
        ThreadPool.SetMinThreads(initialOptions.ProcessingThreads * 2, initialOptions.ProcessingThreads * 2);

        _incomingChannel = Channel.CreateUnbounded<UdpReceiveResult>(new UnboundedChannelOptions
        {
            SingleWriter = true,
            AllowSynchronousContinuations = true
        });

        _snapshot = BuildStateSnapshot(initialOptions);

        // Bind IOptionsMonitor to dynamic atomic snapshot swapping
        _optionsChangeToken = _optionsMonitor.OnChange(OnOptionsChanged);
    }

    private void OnOptionsChanged(DnsEngineOptions newOptions)
    {
        var nextSnapshot = BuildStateSnapshot(newOptions);
        Interlocked.Exchange(ref _snapshot, nextSnapshot);
        _logger.LogInformation("Hot-reloaded engine snapshot with {Hosts} hosts, {BlockCount} blocked domains, and {UpstreamCount} upstreams.",
            nextSnapshot.Hosts.Count, nextSnapshot.BlockedDomains.Count, nextSnapshot.UpstreamResolvers.Count);
    }

    private static EngineStateSnapshot BuildStateSnapshot(DnsEngineOptions options)
    {
        var hostsBuilder = ImmutableDictionary.CreateBuilder<string, IPAddress>(StringComparer.OrdinalIgnoreCase);
        foreach (var (host, ipStr) in options.Hosts)
        {
            if (IPAddress.TryParse(ipStr, out var ip)) hostsBuilder[host] = ip;
        }

        var ptrBuilder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (ptr, host) in options.PtrRecords)
        {
            ptrBuilder[ptr] = host;
        }

        var upstreamsBuilder = ImmutableList.CreateBuilder<IPAddress>();
        foreach (var ipStr in options.UpstreamResolvers)
        {
            if (IPAddress.TryParse(ipStr, out var ip)) upstreamsBuilder.Add(ip);
        }

        return new EngineStateSnapshot(
            Hosts: hostsBuilder.ToImmutable(),
            PtrRecords: ptrBuilder.ToImmutable(),
            BlockedDomains: options.BlockedDomains.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
            UpstreamResolvers: upstreamsBuilder.ToImmutable()
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.CurrentValue;
        _logger.LogInformation("Starting Optimized DNS Engine on Port {Port}...", options.Port);

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

                _incomingChannel.Writer.TryWrite(new UdpReceiveResult(packetCopy, result.RemoteEndPoint, socket));
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
            var state = Volatile.Read(ref _snapshot);

            if (_cache.TryGet(request.QuestionName, (ushort)request.QuestionType, out var cachedPayload))
            {
                responseBytes = cachedPayload;
                resolutionSource = "CACHE";
                return;
            }

            if (state.BlockedDomains.Contains(request.QuestionName))
            {
                var ede = new ExtendedDnsError
                {
                    InfoCode = ExtendedDnsErrorCode.Filtered,
                    ExtraText = "Blocked by security filter rule"
                };

                responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.Refused, ede: ede);
                resolutionSource = "BLOCKED_EDE";
                return;
            }

            if (request.QuestionType == DnsType.A || request.QuestionType == DnsType.AAAA)
            {
                if (state.Hosts.TryGetValue(request.QuestionName, out var matchedIp))
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
            }

            if (request.QuestionType == DnsType.PTR)
            {
                if (state.PtrRecords.TryGetValue(request.QuestionName, out var hostname))
                {
                    var ptrBuffer = new byte[256];
                    int ptrOffset = 0;
                    DnsWireBuilder.EncodeDomainName(ptrBuffer, ref ptrOffset, hostname);

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
            }

            responseBytes = await ExecuteUpstreamQueryAsync(state, rawPacket, ct).ConfigureAwait(false);
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

    private async Task<byte[]?> ExecuteUpstreamQueryAsync(EngineStateSnapshot state, byte[] rawRequest, CancellationToken ct)
    {
        if (state.UpstreamResolvers.Count == 0) return null;

        var upstreamEp = new IPEndPoint(state.UpstreamResolvers[0], 53);
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

    public override void Dispose()
    {
        _optionsChangeToken?.Dispose();
        base.Dispose();
    }
}
