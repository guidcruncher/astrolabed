using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Protocol;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp.Services;

/// <summary>
/// Default handler for constructing RFC 5905 compliant NTP server response packets.
/// </summary>
/// <param name="optionsMonitor">Monitored NTP server options configuration.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class NtpServerHandler(
    IOptionsMonitor<NtpServerOptions> optionsMonitor,
    ILogger<NtpServerHandler> logger) : INtpServerHandler
{
    private readonly IOptionsMonitor<NtpServerOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly ILogger<NtpServerHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public NtpPacket CreateResponse(NtpPacket requestPacket, DateTimeOffset receiveTime, DateTimeOffset transmitTime)
    {
        ArgumentNullException.ThrowIfNull(requestPacket);

        NtpServerOptions options = _optionsMonitor.CurrentValue;

        LogProcessingRequest(
            _logger,
            requestPacket.VersionNumber,
            requestPacket.TransmitTimestamp.ToDateTimeOffset());

        uint referenceId = NtpPacketSerializer.ConvertReferenceIdToUint(options.ReferenceIdentifier);

        return new NtpPacket
        {
            LeapIndicator = NtpLeapIndicator.NoWarning,
            VersionNumber = requestPacket.VersionNumber,
            Mode = NtpMode.Server,
            Stratum = options.Stratum,
            Poll = requestPacket.Poll,
            Precision = options.Precision,
            RootDelay = options.RootDelay,
            RootDispersion = options.RootDispersion,
            ReferenceIdentifier = referenceId,
            ReferenceTimestamp = NtpTimestamp.FromDateTimeOffset(receiveTime),
            OriginTimestamp = requestPacket.TransmitTimestamp,
            ReceiveTimestamp = NtpTimestamp.FromDateTimeOffset(receiveTime),
            TransmitTimestamp = NtpTimestamp.FromDateTimeOffset(transmitTime)
        };
    }

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Debug,
        Message = "Processing NTP request from client. Version: {Version}, TransmitTimestamp: {ClientTransmit}")]
    private static partial void LogProcessingRequest(ILogger logger, byte version, DateTimeOffset clientTransmit);
}

