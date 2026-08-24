using System.Net;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Defines a contract for a DNS protocol listener capable of accepting and processing incoming transport-level queries.
/// </summary>
public interface IDnsListener
{
    /// <summary>
    /// Asynchronously starts listening for incoming DNS network requests on the specified IP address and port.
    /// </summary>
    /// <param name="address">The local <see cref="IPAddress"/> to bind the listener socket to.</param>
    /// <param name="port">The port number to listen on (typically 53 for standard DNS).</param>
    /// <param name="ct">A token to monitor for cancellation requests to trigger graceful shutdown.</param>
    /// <returns>A <see cref="Task"/> that represents the lifetime of the listening loop.</returns>
    Task ListenAsync(IPAddress address, int port, CancellationToken ct = default);
}
