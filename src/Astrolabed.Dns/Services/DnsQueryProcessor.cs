// File: src/Astrolabed.Dns/Services/DnsQueryProcessor.cs
using System;
using System.Buffers.Binary;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Cache;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;
using Astrolabed.Dns.Resolvers;
using Astrolabed.Dns.Serialization;
using Astrolabed.Dns.Upstream;
using Astrolabed.EventBus;
using Astrolabed.EventBus.Events;
using Astrolabed.Network;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

public sealed class DnsQueryProcessor : IDnsQueryProcessor
{
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;
    private readonly IDnsCache _cache;
    private readonly IDomainFilter _domainFilter;
    private readonly IHostRecordResolver _hostResolver;
    private readonly IPtrResolver _ptrResolver;
    private readonly IUpstreamClientFactory _upstreamClientFactory;
    private readonly ILogger<DnsQueryProcessor> _logger;
    private readonly IInProcEventBroker _eventBus;
    private readonly IClientNameResolver _clientResolver;

    public DnsQueryProcessor(
        IOptionsMonitor<DnsEngineOptions> optionsMonitor,
        IDnsCache cache,
        IDomainFilter domainFilter,
        IHostRecordResolver hostResolver,
        IPtrResolver ptrResolver,
        IUpstreamClientFactory upstreamClientFactory,
        IInProcEventBroker eventBus,
    IClientNameResolver clientResolver,
        ILogger<DnsQueryProcessor> logger)
    {
        _optionsMonitor = optionsMonitor;
        _cache = cache;
        _domainFilter = domainFilter;
        _hostResolver = hostResolver;
        _ptrResolver = ptrResolver;
        _clientResolver = clientResolver;
        _upstreamClientFactory = upstreamClientFactory;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<byte[]?> ProcessRequestAsync(byte[] rawPacket, EndPoint clientEndpoint, CancellationToken ct)
    {
        var address = clientEndpoint.GetIPAddress();
        DnsContext context = new DnsContext(address);
        string clientName = "localhost";
        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        //if (!IPAddress.IsLoopback(address))
        //{
        clientName = "";
        var ptrQuery = address.ToPtrFormat();
        _logger.LogInformation("Determining Client Name for {PtrQuery}", ptrQuery);
        clientName = await _clientResolver.ResolveClientNameAsync(ptrQuery, ct);
        _logger.LogInformation("ClientName is {ClientName}", clientName);
        //}

        if (!DnsWireParser.TryParse(rawPacket, out var request) || request is null)
        {
            return null;
        }

        byte[]? responseBytes = null;
        string resolutionSource = "UNKNOWN";

        try
        {
            // 1. Cache Check
            if (request.QuestionName is { Length: > 0 } &&
                _cache.TryGet(request.QuestionName, (ushort)request.QuestionType, out var cachedPayload) &&
                cachedPayload is byte[] payloadBytes)
            {
                responseBytes = (byte[])payloadBytes.Clone();
                BinaryPrimitives.WriteUInt16BigEndian(responseBytes.AsSpan(0, 2), request.TransactionId);

                resolutionSource = "CACHE";
                return responseBytes;
            }

            // 2. Blocklist / Allowlist Filter Evaluation
            if (request.QuestionName is { Length: > 0 } &&
                !_domainFilter.IsAllowed(request.QuestionName) &&
                _domainFilter.IsBlocked(request.QuestionName, out var reason))
            {
                var filterEde = new ExtendedDnsError
                {
                    InfoCode = ExtendedDnsErrorCode.Filtered,
                    ExtraText = reason ?? "Blocked by policy filter"
                };

                var options = _optionsMonitor.CurrentValue;

                switch (options.BlockedResponseMode)
                {
                    case BlockedResponseMode.NxDomain:
                        responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NXDomain, ede: filterEde);
                        resolutionSource = "BLOCKED_NXDOMAIN";
                        break;

                    case BlockedResponseMode.ServFail:
                        responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.ServFail, ede: filterEde);
                        resolutionSource = "BLOCKED_SERVFAIL";
                        break;

                    case BlockedResponseMode.ZeroIp:
                        var zeroIp = request.QuestionType == DnsType.AAAA ? IPAddress.IPv6Any : IPAddress.Any;
                        var zeroRecord = new DnsResourceRecord
                        {
                            Name = request.QuestionName,
                            Type = request.QuestionType,
                            Class = 1,
                            Ttl = 60,
                            ParsedIp = zeroIp
                        };
                        responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NoError, [zeroRecord], filterEde);
                        resolutionSource = "BLOCKED_ZERO_IP";
                        break;

                    case BlockedResponseMode.CustomIp:
                        if (IPAddress.TryParse(options.CustomBlockedIp, out var customIp))
                        {
                            var customRecord = new DnsResourceRecord
                            {
                                Name = request.QuestionName,
                                Type = request.QuestionType,
                                Class = 1,
                                Ttl = 60,
                                ParsedIp = customIp
                            };
                            responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NoError, [customRecord], filterEde);
                            resolutionSource = "BLOCKED_CUSTOM_IP";
                        }
                        else
                        {
                            responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.Refused, ede: filterEde);
                            resolutionSource = "BLOCKED_REFUSED_FALLBACK";
                        }
                        break;

                    case BlockedResponseMode.Refused:
                    default:
                        responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.Refused, ede: filterEde);
                        resolutionSource = "BLOCKED_REFUSED";
                        break;
                }

                return responseBytes;
            }

            // 3. Hosts File Resolution (A / AAAA)
            if (request.QuestionName is { Length: > 0 } &&
                (request.QuestionType == DnsType.A || request.QuestionType == DnsType.AAAA) &&
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

                responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NoError, [record]);
                resolutionSource = "HOSTS_FILE";
                return responseBytes;
            }

            // 4. Reverse PTR Lookup Resolution
            if (request.QuestionType == DnsType.PTR && request.QuestionName is { Length: > 0 })
            {
                // 4a. Static Overrides Match
                if (_ptrResolver.TryResolvePtr(request.QuestionName, out var targetDomain) && targetDomain is not null)
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

                    responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NoError, [record]);
                    resolutionSource = "LOCAL_PTR";
                    return responseBytes;
                }

                // 4b. Conditional PTR Subnet Forwarding
                if (_ptrResolver is PtrResolver concreteResolver &&
                    concreteResolver.TryGetConditionalForwarder(request.QuestionName, out var targetResolverIp) &&
                    targetResolverIp is not null)
                {
                    var upstreamMessage = await _upstreamClientFactory.ExecuteQueryAsync(targetResolverIp.ToString(), rawPacket, ct).ConfigureAwait(false);

                    if (upstreamMessage is not null)
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
            if (upstreams is { Count: > 0 })
            {
                foreach (var upstream in upstreams)
                {
                    try
                    {
                        var upstreamMessage = await _upstreamClientFactory.ExecuteQueryAsync(upstream, rawPacket, ct).ConfigureAwait(false);

                        if (upstreamMessage is not null)
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
                        _logger.LogWarning(ex, "[Context {Context}] Failed to resolve query via upstream {Upstream}", context.Id.ToString(), upstream);
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
            _logger.LogInformation("Context [{Context}] Query [{Domain} | {Type}] Client: {Client} Source: {Source} Elapsed: {Elapsed:F2}ms",
                context.Id.ToString(), request.QuestionName, request.QuestionType, clientEndpoint, resolutionSource, (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);

            var dnsEvent = new DnsResponseEvent(
                startTime,
                context.Id.ToString(),
                request.QuestionName,
                request.QuestionType.ToString().ToUpperInvariant(),
                clientEndpoint,
                clientName,
                resolutionSource,
                (DateTimeOffset.UtcNow - startTime).TotalMilliseconds
            );

            await _eventBus.PublishAsync(dnsEvent).ConfigureAwait(false);
        }
    }
}
