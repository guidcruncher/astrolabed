using System.Net;

namespace Astrolabed.Data.Models;

/// <summary>
/// Represents the database entity for a DHCP lease, mapping exact SQL column representations
/// where timestamps are stored as Unix epoch seconds (bigint) and booleans as integers (0 or 1).
/// </summary>
public sealed class DhcpLeaseEntity
{
    public required string ClientId { get; set; }
    public required string ClientName { get; set; }
    public required string MacAddress { get; set; }
    public required string IpAddress { get; set; }
    public required long LeaseStartTime { get; set; }
    public required long LeaseEndTime { get; set; }
    public required int IsActive { get; set; }

    public DhcpLease ToDomain()
    {
        return new DhcpLease
        {
            ClientId = ClientId,
            ClientName = ClientName,
            MacAddress = MacAddress,
            IpAddress = IPAddress.Parse(IpAddress),
            LeaseStartTime = DateTimeOffset.FromUnixTimeSeconds(LeaseStartTime).UtcDateTime,
            LeaseEndTime = DateTimeOffset.FromUnixTimeSeconds(LeaseEndTime).UtcDateTime,
            IsActive = IsActive == 1
        };
    }

    public static DhcpLeaseEntity FromDomain(DhcpLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);

        long startTimeEpoch = new DateTimeOffset(DateTime.SpecifyKind(lease.LeaseStartTime, DateTimeKind.Utc)).ToUnixTimeSeconds();
        long endTimeEpoch = new DateTimeOffset(DateTime.SpecifyKind(lease.LeaseEndTime, DateTimeKind.Utc)).ToUnixTimeSeconds();

        return new DhcpLeaseEntity
        {
            ClientId = lease.ClientId,
            ClientName = lease.ClientName,
            MacAddress = lease.MacAddress,
            IpAddress = lease.IpAddress.ToString(),
            LeaseStartTime = startTimeEpoch,
            LeaseEndTime = endTimeEpoch,
            IsActive = lease.IsActive ? 1 : 0
        };
    }
}
