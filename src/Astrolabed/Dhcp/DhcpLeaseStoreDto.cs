using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astrolabed.Dhcp;

internal sealed class DhcpLeaseStoreDto
{
    [JsonPropertyName("Leases")]
    public List<DhcpLeaseDto> Leases { get; set; } = [];

    [JsonPropertyName("BadIps")]
    public List<string> BadIps { get; set; } = [];
}

