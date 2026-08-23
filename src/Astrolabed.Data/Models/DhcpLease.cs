using System.Net;

namespace Astrolabed.Data.Models;

/// <summary>
/// Domain model representing an active or historic DHCP address lease assigned to a network client.
/// </summary>
public class DhcpLease
{
    /// <summary>
    /// Gets or sets the client identifier string (e.g., Option 61 payload or MAC fallback identifier).
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the network client host name associated with the lease.
    /// </summary>
    public required string ClientName { get; set; }

    /// <summary>
    /// Gets or sets the hardware MAC address of the leased network client interface.
    /// </summary>
    public required string MacAddress { get; set; }

    /// <summary>
    /// Gets or sets the IP address leased to the client interface.
    /// </summary>
    public required IPAddress IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the lease was issued or renewed.
    /// </summary>
    public DateTime LeaseStartTime { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the lease expires.
    /// </summary>
    public DateTime LeaseEndTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the lease is currently active.
    /// </summary>
    /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
    public bool IsActive { get; set; }
}
