using System.Net;

namespace Astrolabed.Data;

public class DhcpLease
{
    public required string ClientId { get; set; }
    public required string ClientName {get; set; }
    public required string MacAddress { get; set; }
    public required IPAddress IpAddress { get; set; }
    public DateTime LeaseStartTime { get; set; }
    public DateTime LeaseEndTime { get; set; }
    public bool IsActive { get; set; }
}
