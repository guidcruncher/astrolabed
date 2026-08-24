using Astrolabed.Ntp.Protocol;

namespace Astrolabed.Ntp.Services;

/// <summary>
/// Defines the processing contract for crafting RFC 5905 compliant NTP server response packets.
/// </summary>
public interface INtpServerHandler
{
    /// <summary>
    /// Constructs a valid NTP server response packet derived from an incoming client request and recorded receipt/transmission timestamps.
    /// </summary>
    /// <param name="requestPacket">The incoming, decoded NTP client request packet.</param>
    /// <param name="receiveTime">The high-precision timestamp recorded when the client packet arrived at the server socket interface.</param>
    /// <param name="transmitTime">The high-precision timestamp recorded immediately prior to transmitting the response packet back to the client.</param>
    /// <returns>An <see cref="NtpPacket"/> populated with server state, stratum info, and precise timestamps ready for serialization.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestPacket"/> is null.</exception>
    NtpPacket CreateResponse(NtpPacket requestPacket, DateTimeOffset receiveTime, DateTimeOffset transmitTime);
}
