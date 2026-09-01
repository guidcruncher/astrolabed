// File: src/Astrolabed.Dns/Services/DnsQueryProcessor.cs
using System.Buffers.Binary;
using System.Net;

using Astrolabed.Core.Network;
using Astrolabed.Dns.Cache;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;
using Astrolabed.Dns.Resolvers;
using Astrolabed.Dns.Serialization;
using Astrolabed.Dns.Upstream;
using Astrolabed.EventBus;
using Astrolabed.EventBus.Events;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

/// <summary>
/// High-performance processor for handling DNS queries, blocklist filter evaluation, local host resolution, and upstream forwarding.
/// </summary>
/// <param name="optionsMonitor">Monitored engine configuration options.</param>
/// <param name="cache">DNS query response cache.</param>
/// <param name="domainFilter">Domain blocklist/allowlist filter component.</param>
/// <param name="hostResolver">Local hosts file record resolver.</param>
/// <param name="ptrResolver">Reverse DNS PTR record resolver.</param>
/// <param name="upstreamClientFactory">Upstream DNS resolution client factory.</param>
/// <param name="eventBus">In-process event bus for publishing DNS telemetry metrics.</param>
/// <param name="clientResolver">Client IP reverse name lookup resolver.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class DnsQueryProcessor(
    IOptionsMonitor<DnsEngineOptions> optionsMonitor,
    IDnsCache cache,
    IDomainFilter domainFilter,
    IHostRecordResolver hostResolver,
    IPtrResolver ptrResolver,
    IUpstreamClientFactory upstreamClientFactory,
    IInProcEventBroker eventBus,
    IClientNameResolver clientResolver,
    ILogger<DnsQueryProcessor> logger) : IDnsQueryProcessor
{
    private static readonly AsyncLocal<bool> IsInternalLookup = new();

    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly IDnsCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly IDomainFilter _domainFilter = domainFilter ?? throw new ArgumentNullException(nameof(domainFilter));
    private readonly IHostRecordResolver _hostResolver = hostResolver ?? throw new ArgumentNullException(nameof(hostResolver));
    private readonly IPtrResolver _ptrResolver = ptrResolver ?? throw new ArgumentNullException(nameof(ptrResolver));
    private readonly IUpstreamClientFactory _upstreamClientFactory = upstreamClientFactory ?? throw new ArgumentNullException(nameof(upstreamClientFactory));
    private readonly IInProcEventBroker _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    private readonly IClientNameResolver _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
    private readonly ILogger<DnsQueryProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<byte[]?> ProcessRequestAsync(ReadOnlyMemory<byte> rawPacket, EndPoint clientEndpoint, CancellationToken ct)
    {
        bool isNestedQuery = IsInternalLookup.Value;
        bool blocked = false;
        IPAddress address = clientEndpoint.GetIPAddress();
        var context = new DnsContext(address);
        string clientName = "localhost";
        string upstreamSource = "";
        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        if (!isNestedQuery && !IPAddress.IsLoopback(address))
        {
            string ptrQuery = address.ToPtrFormat();
            LogDeterminingClientName(_logger, ptrQuery);

            IsInternalLookup.Value = true;
            try
            {
                clientName = await _clientResolver.ResolveClientNameAsync(ptrQuery, ct).ConfigureAwait(false);
            }
            finally
            {
                IsInternalLookup.Value = false;
            }

            LogClientNameResolved(_logger, clientName);
        }

        if (!DnsWireParser.TryParse(rawPacket.Span, out DnsWireMessage? request) || request is null)
        {
            return null;
        }

        byte[]? responseBytes = null;
        string resolutionSource = "UNKNOWN";
        string rCode = "NOERROR";
        int? blockRuleId = 0;
        IEnumerable<DnsResourceRecord>? answerData = null;

        try
        {
            // 1. Cache Check
            if (request.QuestionName is { Length: > 0 } &&
                _cache.TryGet(request.QuestionName, (ushort)request.QuestionType, out ReadOnlyMemory<byte> cachedPayload))
            {
                responseBytes = cachedPayload.ToArray();
                BinaryPrimitives.WriteUInt16BigEndian(responseBytes.AsSpan(0, 2), request.TransactionId);

                resolutionSource = "CACHE";
                return responseBytes;
            }

            // 2. Blocklist / Allowlist Filter Evaluation
            if (request.QuestionName is { Length: > 0 } &&
                !_domainFilter.IsAllowed(request.QuestionName, out blockRuleId) &&
                _domainFilter.IsBlocked(request.QuestionName, out string? reason, out blockRuleId))
            {
                blocked = true;
                var filterEde = new ExtendedDnsError
                {
                    InfoCode = ExtendedDnsErrorCode.Filtered,
                    ExtraText = reason ?? "Blocked by policy filter"
                };

                DnsEngineOptions options = _optionsMonitor.CurrentValue;

                switch (options.BlockedResponseMode)
                {
                    case BlockedResponseMode.NxDomain:
                        responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NXDomain, ede: filterEde);
                        resolutionSource = "BLOCKED";
                        rCode = "NXDOMAIN";
                        break;

                    case BlockedResponseMode.ServFail:
                        responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.ServFail, ede: filterEde);
                        resolutionSource = "BLOCKED";
                        rCode = "SERVFAIL";
                        break;

                    case BlockedResponseMode.ZeroIp:
                        IPAddress zeroIp = request.QuestionType == DnsType.AAAA ? IPAddress.IPv6Any : IPAddress.Any;
                        var zeroRecord = new DnsResourceRecord
                        {
                            Name = request.QuestionName,
                            Type = request.QuestionType,
                            Class = 1,
                            Ttl = 60,
                            ParsedIp = zeroIp
                        };
                        responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NoError, [zeroRecord], filterEde);
                        resolutionSource = "BLOCKED";
                        rCode = "NOERROR";
                        break;

                    case BlockedResponseMode.CustomIp:
                        if (IPAddress.TryParse(options.CustomBlockedIp, out IPAddress? customIp))
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
                            resolutionSource = "BLOCKED";
                            rCode = "NOERROR";
                        }
                        else
                        {
                            responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.Refused, ede: filterEde);
                            resolutionSource = "BLOCKED";
                            rCode = "REFUSED_FALLBACK";
                        }
                        break;

                    case BlockedResponseMode.Refused:
                    default:
                        responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.Refused, ede: filterEde);
                        resolutionSource = "BLOCKED";
                        rCode = "REFUSED";
                        break;
                }

                return responseBytes;
            }

            // 3. Hosts File Resolution (A / AAAA)
            if (request.QuestionName is { Length: > 0 } &&
                (request.QuestionType == DnsType.A || request.QuestionType == DnsType.AAAA) &&
                _hostResolver.TryResolveHost(request.QuestionName, request.QuestionType, out IPAddress? matchedIp))
            {
                var record = new DnsResourceRecord
                {
                    Name = request.QuestionName,
                    Type = request.QuestionType,
                    Class = 1, // IN (Internet)
                    Ttl = 300,
                    ParsedIp = matchedIp
                };

                answerData = [record];
                responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NoError, [record]);
                resolutionSource = "HOSTS_FILE";
                rCode = "NOERROR";
                return responseBytes;
            }

            // 4. Reverse PTR Lookup Resolution
            if (request.QuestionType == DnsType.PTR && request.QuestionName is { Length: > 0 })
            {
                // 4a. Static Overrides Match
                if (_ptrResolver.TryResolvePtr(request.QuestionName, out string? targetDomain) && targetDomain is not null)
                {
                    Span<byte> ptrBuffer = stackalloc byte[256];
                    int ptrOffset = 0;
                    DnsWireBuilder.EncodeDomainName(ptrBuffer, ref ptrOffset, targetDomain);

                    var record = new DnsResourceRecord
                    {
                        Name = request.QuestionName,
                        Type = DnsType.PTR,
                        Ttl = 300,
                        Data = ptrBuffer[..ptrOffset].ToArray()
                    };

                    answerData = [record];
                    responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.NoError, [record]);
                    resolutionSource = "LOCAL_PTR";
                    rCode = "NOERROR";
                    return responseBytes;
                }

                // 4b. Conditional PTR Subnet Forwarding
                if (_ptrResolver.TryGetConditionalForwarder(request.QuestionName, out IPAddress? targetResolverIp) &&
                    targetResolverIp is not null)
                {
                    DnsWireMessage? upstreamMessage = await _upstreamClientFactory
                        .ExecuteQueryAsync(targetResolverIp.ToString(), rawPacket, ct)
                        .ConfigureAwait(false);

                    if (upstreamMessage is not null)
                    {
                        upstreamSource = targetResolverIp.ToString();
                        upstreamMessage.TransactionId = request.TransactionId;
                        responseBytes = DnsWireBuilder.BuildResponse(upstreamMessage, upstreamMessage.ResponseCode, upstreamMessage.Answers);
                        answerData = upstreamMessage.Answers;
                        resolutionSource = "CONDITIONAL_PTR_UPSTREAM";
                        rCode = "NOERROR";
                        _cache.Store(request.QuestionName, (ushort)request.QuestionType, responseBytes, TimeSpan.FromMinutes(5));
                        return responseBytes;
                    }
                }
            }

            // 5. Default Upstream Forwarding
            IReadOnlyList<string>? upstreams = _optionsMonitor.CurrentValue.UpstreamResolvers;
            if (upstreams is { Count: > 0 })
            {
                foreach (string upstream in upstreams)
                {
                    try
                    {
                        DnsWireMessage? upstreamMessage = await _upstreamClientFactory
                            .ExecuteQueryAsync(upstream, rawPacket, ct)
                            .ConfigureAwait(false);

                        if (upstreamMessage is not null)
                        {
                            upstreamMessage.TransactionId = request.TransactionId;
                            answerData = upstreamMessage.Answers;
                            responseBytes = DnsWireBuilder.BuildResponse(upstreamMessage, upstreamMessage.ResponseCode, upstreamMessage.Answers);
                            resolutionSource = "UPSTREAM";
                            rCode = "NOERROR";
                            _cache.Store(request.QuestionName, (ushort)request.QuestionType, responseBytes, TimeSpan.FromMinutes(5));
                            upstreamSource = upstream;
                            return responseBytes;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUpstreamResolutionFailed(_logger, ex, context.Id, upstream);
                    }
                }
            }

            // Fallback: If no hosts match and upstream fails/unreachable, return ServFail
            var servfailEde = new ExtendedDnsError
            {
                InfoCode = ExtendedDnsErrorCode.NoReachableAuthority,
                ExtraText = "No host entry match and upstream resolvers unreachable"
            };

            responseBytes = DnsWireBuilder.BuildResponse(request, DnsResponseCode.ServFail, ede: servfailEde);
            resolutionSource = "FALLBACK";
            rCode = "SERVFAIL";
            return responseBytes;
        }
        finally
        {
            if (!isNestedQuery)
            {
                int ttlSeconds = 300;
                if ((answerData != null) && (answerData.Count() < 0))
                {
                    ttlSeconds = (int)(answerData.ToArray()[0].Ttl);
                }

                double elapsedMs = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                var dnsEvent = new DnsResponseEvent(
                    startTime,
                    context.Id.ToString(),
                    request.QuestionName,
                    request.QuestionType.ToString().ToUpperInvariant(),
                    ((IPEndPoint)clientEndpoint),
                    clientName,
                    resolutionSource,
                    rCode,
                    elapsedMs,
                    blocked,
                    upstreamSource,
                    answerData?.ToAnswerData(),
                    ttlSeconds,
                    blockRuleId
                );

                await _eventBus.PublishAsync(dnsEvent).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<DnsWireMessage?> ProcessQueryAsync(
        string domain,
        DnsType type,
        EndPoint clientEndpoint,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        ArgumentNullException.ThrowIfNull(clientEndpoint);

        // 1. Transaction ID (random or arbitrary for generated packet)
        ushort transactionId = (ushort)Random.Shared.Next(0, ushort.MaxValue + 1);

        var rawPacket = QuestionBuilder.BuildQuery(domain, type, transactionId);

        var res = await ProcessRequestAsync(rawPacket, clientEndpoint, ct);

        if (res == null)
        {
            return null;
        }

        if (DnsWireParser.TryParse(res, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    [LoggerMessage(
        EventId = 901,
        Level = LogLevel.Information,
        Message = "Determining Client Name for {PtrQuery}")]
    private static partial void LogDeterminingClientName(ILogger logger, string ptrQuery);

    [LoggerMessage(
        EventId = 902,
        Level = LogLevel.Information,
        Message = "ClientName is {ClientName}")]
    private static partial void LogClientNameResolved(ILogger logger, string clientName);

    [LoggerMessage(
        EventId = 903,
        Level = LogLevel.Warning,
        Message = "[Context {ContextId}] Failed to resolve query via upstream {Upstream}")]
    private static partial void LogUpstreamResolutionFailed(ILogger logger, Exception exception, Guid contextId, string upstream);
}
