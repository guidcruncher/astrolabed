using Astrolabed.Dhcp.Protocol;

namespace Astrolabed.Dhcp.Services;

/// <summary>
/// Defines the processing contract for handling incoming DHCP protocol requests and determining reply responses.
/// </summary>
public interface IDhcpHandler
{
    /// <summary>
    /// Processes an incoming <see cref="DhcpMessage"/> and generates an appropriate DHCP reply message.
    /// </summary>
    /// <param name="request">The incoming, decoded DHCP client message payload.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task containing the reply <see cref="DhcpMessage"/> (such as OFFER, ACK, or NAK) to transmit back to the client; 
    /// or <see langword="null"/> if no message should be sent (e.g., for RELEASE or corrupt messages).
    /// </returns>
    Task<DhcpMessage?> ProcessMessageAsync(DhcpMessage request, CancellationToken cancellationToken = default);
}
