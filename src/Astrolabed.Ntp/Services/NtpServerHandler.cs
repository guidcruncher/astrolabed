using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Protocol;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp.Services;

public class NtpServerHandler : INtpServerHandler
{
    private readonly IOptionsMonitor<NtpServerOptions> _optionsMonitor;
    private readonly ILogger<NtpServerHandler> _logger;

    public NtpServerHandler(
        IOptionsMonitor<NtpServerOptions> optionsMonitor,
        ILogger<NtpServerHandler> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public NtpPacket CreateResponse(NtpPacket requestPacket, DateTimeOffset receiveTime, DateTimeOffset transmitTime)
    {
        NtpServerOptions options = _optionsMonitor.CurrentValue;

        _logger.LogDebug(
            "Processing NTP request from client. Version: {Version}, TransmitTimestamp: {ClientTransmit}",
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
}

