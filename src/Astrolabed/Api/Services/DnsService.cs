using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Data;
using Astrolabed.Dns;
using Astrolabed.Dns.Core;
using Astrolabed.Dns.RuleEngine;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Api.Services;

public sealed class DnsService : IDnsService
{
    private readonly ILogger<DnsService> _logger;
    private readonly DnsForwarderOptions _options;
    private readonly IDnsCache _dnsCache;
    private readonly DnsForwarderService _forwarder;

    public DnsService(
        ILogger<DnsService> logger,
    DnsForwarderService forwarder,
        IDnsCache dnsCache,
        IOptions<DnsForwarderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _dnsCache = dnsCache;
        _logger = logger;
        _options = options.Value;
        _forwarder = forwarder;
    }

    public void FlushCache()
    {
        _dnsCache.Flush();
    }

    public PagedResult<DnsResponse> GetCachedResponsesPaged(int pageNumber = 1, int pageSize = 10, string? search = null)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var cachedResponses = _dnsCache.GetCachedResponses();

        if (!string.IsNullOrWhiteSpace(search))
        {
            cachedResponses = cachedResponses.Where(r =>
                r.QueryName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.QueryType.Equals(search, StringComparison.OrdinalIgnoreCase));
        }

        var allList = cachedResponses.ToList();
        int totalCount = allList.Count;

        var items = allList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<DnsResponse>(
            Items: items,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: pageSize
        );
    }

    public Task<DnsResponse> QueryAsync(
        string name,
        string type = "A",
        CancellationToken cancellationToken = default)
    {
        var addressStr = _options.Listen?.Address;
        if (!IPAddress.TryParse(addressStr, out var address))
        {
            address = IPAddress.Loopback;
        }

        int port = _options.Listen?.Port > 0 ? _options.Listen.Port : 53;
        var endpoint = new IPEndPoint(address, port);

        return QueryServerAsync(name, type, endpoint, cancellationToken);
    }

    public async Task<DnsResponse> QueryServerAsync(
        string name,
        string type,
        IPEndPoint endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(endpoint);

        _logger.LogInformation("Executing DNS query ({Type}) for '{Name}' at {Endpoint}", type, name, endpoint);

        var sw = Stopwatch.StartNew();

        try
        {
            // 1. Build standard DNS query wire format packet
            var requestBytes = BuildDnsQueryPacket(name, type);
            var response = await _forwarder.ProcessAsync(
                    requestBytes,
                    IPEndPoint.Parse("127.0.0.1"),
                    "localhost", cancellationToken).ConfigureAwait(false);
            // 2. Transmit and receive via UDP

            sw.Stop();

            var resp = DnsResponseDeserializer.Deserialize(response, endpoint.Address.ToString(), sw.Elapsed, name, type);
            if (resp == null)
            {
                resp = new DnsResponse
                {
                    Success = false,
                    Server = endpoint.ToString(),
                    QueryName = name,
                    QueryType = type.ToUpperInvariant(),
                    ResponseCode = "SERVFAIL",
                    Elapsed = sw.Elapsed,
                    ErrorMessage = "",
                    Header = CreateFallbackHeader(),
                    Answers = Array.Empty<DnsResource>(),
                    Authorities = Array.Empty<DnsResource>(),
                    Additionals = Array.Empty<DnsResource>()
                };
            }
            return resp;

        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error executing DNS query for {Name} ({Type})", name, type);

            return new DnsResponse
            {
                Success = false,
                Server = endpoint.ToString(),
                QueryName = name,
                QueryType = type.ToUpperInvariant(),
                ResponseCode = "SERVFAIL",
                Elapsed = sw.Elapsed,
                ErrorMessage = ex.Message,
                Header = CreateFallbackHeader(),
                Answers = Array.Empty<DnsResource>(),
                Authorities = Array.Empty<DnsResource>(),
                Additionals = Array.Empty<DnsResource>()
            };
        }
    }

    private static byte[] BuildDnsQueryPacket(string name, string typeStr)
    {
        ushort typeCode = GetRecordTypeCode(typeStr);
        ushort transactionId = (ushort)Random.Shared.Next(1, 65535);

        var packet = new List<byte>
        {
            (byte)(transactionId >> 8), (byte)(transactionId & 0xFF), // Transaction ID
            0x01, 0x00,                                               // Flags: Standard query with Recursion Desired
            0x00, 0x01,                                               // Questions: 1
            0x00, 0x00,                                               // Answer RRs: 0
            0x00, 0x00,                                               // Authority RRs: 0
            0x00, 0x00                                                // Additional RRs: 0
        };

        // QNAME encoding (e.g. "example.com" -> 7 "example" 3 "com" 0)
        var labels = name.Trim('.').Split('.');
        foreach (var label in labels)
        {
            var bytes = Encoding.ASCII.GetBytes(label);
            packet.Add((byte)bytes.Length);
            packet.AddRange(bytes);
        }
        packet.Add(0x00); // End of QNAME

        // QTYPE
        packet.Add((byte)(typeCode >> 8));
        packet.Add((byte)(typeCode & 0xFF));

        // QCLASS (IN = 1)
        packet.Add(0x00);
        packet.Add(0x01);

        return packet.ToArray();
    }

    private static ushort GetRecordTypeCode(string type) => type.ToUpperInvariant() switch
    {
        "A" => 1,
        "NS" => 2,
        "CNAME" => 5,
        "SOA" => 6,
        "PTR" => 12,
        "MX" => 15,
        "TXT" => 16,
        "AAAA" => 28,
        "SRV" => 33,
        "ANY" => 255,
        _ => 1
    };

    private static DnsHeader ParseDnsHeader(byte[] buffer)
    {
        if (buffer is null || buffer.Length < 12)
        {
            return CreateFallbackHeader();
        }

        ushort id = (ushort)((buffer[0] << 8) | buffer[1]);
        ushort flags = (ushort)((buffer[2] << 8) | buffer[3]);

        return new DnsHeader
        {
            TransactionId = id,
            IsResponse = (flags & 0x8000) != 0,
            OpCode = ((flags >> 11) & 0x0F) switch
            {
                0 => "QUERY",
                1 => "IQUERY",
                2 => "STATUS",
                _ => "UNKNOWN"
            },
            AuthoritativeAnswer = (flags & 0x0400) != 0,
            Truncated = (flags & 0x0200) != 0,
            RecursionDesired = (flags & 0x0100) != 0,
            RecursionAvailable = (flags & 0x0080) != 0,
            AuthenticData = (flags & 0x0020) != 0,
            CheckingDisabled = (flags & 0x0010) != 0,
            QuestionCount = (ushort)((buffer[4] << 8) | buffer[5]),
            AnswerCount = (ushort)((buffer[6] << 8) | buffer[7]),
            NameServerCount = (ushort)((buffer[8] << 8) | buffer[9]),
            AdditionalCount = (ushort)((buffer[10] << 8) | buffer[11])
        };
    }

    private static DnsHeader CreateFallbackHeader() => new()
    {
        TransactionId = 0,
        IsResponse = true,
        OpCode = "QUERY",
        AuthoritativeAnswer = false,
        Truncated = false,
        RecursionDesired = true,
        RecursionAvailable = false,
        AuthenticData = false,
        CheckingDisabled = false,
        QuestionCount = 1,
        AnswerCount = 0,
        NameServerCount = 0,
        AdditionalCount = 0
    };
}
