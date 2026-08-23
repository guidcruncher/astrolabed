using System.Net;

using Astrolabed.Data.Models;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Defines the persistence contract for managing DHCP lease allocations, updates, and releases
/// in accordance with RFC 2131 (DHCPv4) and RFC 8415 (DHCPv6) specifications.
/// </summary>
public interface IDhcpLeaseRepository
{
    /// <summary>
    /// Asynchronously retrieves a DHCP lease matching either the specified Client Identifier or MAC address.
    /// </summary>
    /// <param name="clientId">The unique client identifier (e.g., DUID or Option 61 value).</param>
    /// <param name="macAddress">The hardware MAC address of the client interface.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>

    /// <returns>
    /// A task representing the asynchronous operation, containing the matching <see cref="DhcpLease"/> if found; 
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientId"/> or <paramref name="macAddress"/> is null or whitespace.</exception>
    Task<DhcpLease?> GetLeaseByClientIdOrMacAsync(
        string clientId,
        string macAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves an active DHCP lease assigned to the specified IP address.
    /// </summary>
    /// <param name="ipAddress">The target <see cref="IPAddress"/> to look up.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the matching <see cref="DhcpLease"/> if found and active; 
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ipAddress"/> is null.</exception>
    Task<DhcpLease?> GetLeaseByIpAsync(
        IPAddress ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously determines whether an IP address is available for allocation to the requesting client.
    /// </summary>
    /// <param name="ipAddress">The requested <see cref="IPAddress"/>.</param>
    /// <param name="clientId">The unique client identifier making the request.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing <see langword="true"/> if the IP address is available 
    /// or already assigned to the requesting client; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ipAddress"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientId"/> is null or whitespace.</exception>
    Task<bool> IsIpAvailableAsync(
        IPAddress ipAddress,
        string clientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously allocates a new DHCP lease or updates an existing lease duration for a client.
    /// </summary>
    /// <param name="clientId">The unique client identifier.</param>
    /// <param name="clientName">The host name announced by the client.</param>
    /// <param name="macAddress">The hardware MAC address of the requesting client.</param>
    /// <param name="requestedIp">The requested or assigned <see cref="IPAddress"/>.</param>
    /// <param name="duration">The lease duration period (lease time).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the resulting allocated or updated <see cref="DhcpLease"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientId"/> or <paramref name="macAddress"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientName"/> or <paramref name="requestedIp"/> is null.</exception>
    Task<DhcpLease> AllocateOrUpdateLeaseAsync(
        string clientId,
        string clientName,
        string macAddress,
        IPAddress requestedIp,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously marks an active DHCP lease as released or inactive (DHCPRELEASE message processing).
    /// </summary>
    /// <param name="clientId">The unique client identifier of the releasing client.</param>
    /// <param name="macAddress">The hardware MAC address of the releasing client.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous completion of the release operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientId"/> or <paramref name="macAddress"/> is null or whitespace.</exception>
    Task ReleaseLeaseAsync(
        string clientId,
        string macAddress,
        CancellationToken cancellationToken = default);
}
