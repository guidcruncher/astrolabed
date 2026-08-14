using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Api.Services;

public sealed class DnsService : IDnsService
{
    private readonly ILogger<DnsService> _logger;
    private readonly IDnsRequestHandler _handler;
    private readonly DnsForwarderOptions _options;

    public DnsService(
        ILogger<DnsService> logger,
        IDnsRequestHandler handler,
        IOptions<DnsForwarderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _handler = handler;
        _options = options.Value;
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

        using var udp = new UdpClient(endpoint.AddressFamily);

        try
        {
            udp.Connect(endpoint);

            // 1. Build standard DNS query wire format packet
            var queryBytes = BuildDnsQueryPacket(name, type);

            // 2. Transmit and receive via UDP
            await udp.SendAsync(queryBytes, cancellationToken).ConfigureAwait(false);
            var receiveResult = await udp.ReceiveAsync(cancellationToken).ConfigureAwait(false);

            sw.Stop();

            // 3. Delegate response handling/parsing using internal IDnsRequestHandler
            var handlerResult = await _handler.HandleAsync(receiveResult.Buffer, endpoint, cancellationToken).ConfigureAwait(false);

            return new DnsResponse
            {
                Success = handlerResult.Success,
                Server = endpoint.ToString(),
                QueryName = name,
                QueryType = type.ToUpperInvariant(),
                ResponseCode = handlerResult.ResponseCode ?? "NOERROR",
                Elapsed = sw.Elapsed,
                Header = ParseDnsHeader(receiveResult.Buffer),
                Answers = handlerResult.Answers ?? Array.Empty<DnsResourceRecord>(),
                Authorities = handlerResult.Authorities ?? Array.Empty<DnsResourceRecord>(),
                Additionals = handlerResult.Additionals ?? Array.Empty<DnsResourceRecord>()
            };
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
                Answers = Array.Empty<DnsResourceRecord>(),
                Authorities = Array.Empty<DnsResourceRecord>(),
                Additionals = Array.Empty<DnsResourceRecord>()
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
