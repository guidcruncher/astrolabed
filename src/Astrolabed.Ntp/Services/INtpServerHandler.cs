using Astrolabed.Ntp.Protocol;

namespace Astrolabed.Ntp.Services;

public interface INtpServerHandler
{
    NtpPacket CreateResponse(NtpPacket requestPacket, DateTimeOffset receiveTime, DateTimeOffset transmitTime);
}
