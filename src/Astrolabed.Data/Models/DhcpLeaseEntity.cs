using System.Net;

using Astrolabed.Core.Network;

namespace Astrolabed.Data.Models;

/// <summary>
/// Represents the database entity for a DHCP lease, mapping exact SQL column representations
/// where timestamps are stored as Unix epoch seconds (bigint), booleans as integers (0 or 1),
/// and MAC addresses are formatted in colon-separated notation (11:22:33:44:55:66).
/// </summary>
public sealed class DhcpLeaseEntity
{
    /// <summary>
    /// Gets or sets the primary client identifier string column.
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the resolved client hostname column.
    /// </summary>
    public required string ClientName { get; set; }

    /// <summary>
    /// Gets or sets the formatted physical MAC address string column.
    /// </summary>
    public required string MacAddress { get; set; }

    /// <summary>
    /// Gets or sets the IP address string representation column.
    /// </summary>
    public required string IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the UTC lease assignment start timestamp represented in Unix epoch seconds.
    /// </summary>
    public required long LeaseStartTime { get; set; }

    /// <summary>
    /// Gets or sets the UTC lease expiration timestamp represented in Unix epoch seconds.
    /// </summary>
    public required long LeaseEndTime { get; set; }

    /// <summary>
    /// Gets or sets the active status flag column (<c>1</c> for true, <c>0</c> for false).
    /// </summary>
    public required int IsActive { get; set; }

    /// <summary>
    /// Maps this persistence entity instance into a strongly-typed <see cref="DhcpLease"/> domain model.
    /// </summary>
    /// <returns>A new <see cref="DhcpLease"/> domain instance populated with parsed field values.</returns>
    public DhcpLease ToDomain()
    {
        return new DhcpLease
        {
            ClientId = ClientId,
            ClientName = ClientName,
            MacAddress = MacAddressFormatter.Format(MacAddress),
            IpAddress = IPAddress.Parse(IpAddress),
            LeaseStartTime = DateTimeOffset.FromUnixTimeSeconds(LeaseStartTime).UtcDateTime,
            LeaseEndTime = DateTimeOffset.FromUnixTimeSeconds(LeaseEndTime).UtcDateTime,
            IsActive = IsActive == 1
        };
    }

    /// <summary>
    /// Converts a <see cref="DhcpLease"/> domain object into a serializable <see cref="DhcpLeaseEntity"/> relational database record.
    /// </summary>
    /// <param name="lease">The domain model instance to convert.</param>
    /// <returns>A mapped <see cref="DhcpLeaseEntity"/> database entity instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="lease"/> is <c>null</c>.</exception>
    public static DhcpLeaseEntity FromDomain(DhcpLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);

        long startTimeEpoch = new DateTimeOffset(DateTime.SpecifyKind(lease.LeaseStartTime, DateTimeKind.Utc)).ToUnixTimeSeconds();
        long endTimeEpoch = new DateTimeOffset(DateTime.SpecifyKind(lease.LeaseEndTime, DateTimeKind.Utc)).ToUnixTimeSeconds();

        return new DhcpLeaseEntity
        {
            ClientId = lease.ClientId,
            ClientName = lease.ClientName,
            MacAddress = MacAddressFormatter.Format(lease.MacAddress),
            IpAddress = lease.IpAddress.ToString(),
            LeaseStartTime = startTimeEpoch,
            LeaseEndTime = endTimeEpoch,
            IsActive = lease.IsActive ? 1 : 0
        };
    }
}
