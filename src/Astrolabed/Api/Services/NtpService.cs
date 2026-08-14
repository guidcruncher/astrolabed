using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed;
using Astrolabed.Ntp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Api.Services;

public sealed class NtpService : INtpService
{
    private readonly ILogger<NtpService> _logger;
    private readonly INtpRequestHandler _handler;
    private readonly NtpServerOptions _options;

    public NtpService(
        ILogger<NtpService> logger,
        INtpRequestHandler handler,
        IOptions<NtpServerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _handler = handler;
        _options = options.Value;
    }

    public Task<NtpResponse> QueryAsync(CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(_options.ListenAddress, out var address))
        {
            address = IPAddress.Loopback;
        }

        var endpoint = new IPEndPoint(address, _options.Port);
        return QueryServerAsync(endpoint, cancellationToken);
    }

    public async Task<NtpResponse> QueryServerAsync(IPEndPoint endpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        _logger.LogInformation("Querying NTP server at {Endpoint}", endpoint);

        using var udp = new UdpClient(endpoint.AddressFamily);
        if (_options.BufferSize > 0)
        {
            udp.Client.ReceiveBufferSize = _options.BufferSize;
            udp.Client.SendBufferSize = _options.BufferSize;
        }

        try
        {
            udp.Connect(endpoint);

            // 1. Build standard NTP client request frame
            var requestBuffer = CreateClientRequestPacket();

            // 2. Transmit and receive using UdpClient
            await udp.SendAsync(requestBuffer, cancellationToken).ConfigureAwait(false);
            var receiveResult = await udp.ReceiveAsync(cancellationToken).ConfigureAwait(false);

            // 3. Delegate packet processing & validation to internal INtpRequestHandler
            var handlerResult = await _handler.HandleAsync(receiveResult, udp, cancellationToken).ConfigureAwait(false);

            var now = DateTimeOffset.UtcNow;

            return new NtpResponse
            {
                Success = handlerResult.Success,
                Server = endpoint.ToString(),
                SystemTimeUtc = now,
                NetworkTimeUtc = now.Add(handlerResult.Offset),
                Offset = handlerResult.Offset,
                Delay = TimeSpan.Zero,
                Header = ParseHeaderFromBytes(receiveResult.Buffer)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NTP request for {Endpoint}", endpoint);

            return new NtpResponse
            {
                Success = false,
                Server = endpoint.ToString(),
                SystemTimeUtc = DateTimeOffset.UtcNow,
                NetworkTimeUtc = DateTimeOffset.UtcNow,
                Offset = TimeSpan.Zero,
                Delay = TimeSpan.Zero,
                ErrorMessage = ex.Message,
                Header = CreateFallbackHeader()
            };
        }
    }

    private static byte[] CreateClientRequestPacket()
    {
        var packet = new byte[48];
        // Mode 3 (Client) | Version 4
        packet[0] = 0x23;
        return packet;
    }

    private static NtpPacketHeader ParseHeaderFromBytes(byte[] buffer)
    {
        if (buffer is null || buffer.Length < 48)
        {
            return CreateFallbackHeader();
        }

        return new NtpPacketHeader
        {
            LeapIndicator = (buffer[0] & 0xC0) >> 6,
            Version = (buffer[0] & 0x38) >> 3,
            Mode = buffer[0] & 0x07,
            Stratum = buffer[1],
            PollInterval = buffer[2],
            PrecisionSeconds = Math.Pow(2, (sbyte)buffer[3]),
            RootDelayMs = 0,
            RootDispersionMs = 0,
            ReferenceId = string.Empty,
            ReferenceTimestamp = DateTimeOffset.MinValue,
            OriginateTimestamp = DateTimeOffset.MinValue,
            ReceiveTimestamp = DateTimeOffset.MinValue,
            TransmitTimestamp = DateTimeOffset.MinValue
        };
    }

    private static NtpPacketHeader CreateFallbackHeader() => new()
    {
        LeapIndicator = 3,
        Version = 4,
        Mode = 0,
        Stratum = 16,
        PollInterval = 0,
        PrecisionSeconds = 0,
        RootDelayMs = 0,
        RootDispersionMs = 0,
        ReferenceId = "NONE",
        ReferenceTimestamp = DateTimeOffset.MinValue,
        OriginateTimestamp = DateTimeOffset.MinValue,
        ReceiveTimestamp = DateTimeOffset.MinValue,
        TransmitTimestamp = DateTimeOffset.MinValue
    };
}
