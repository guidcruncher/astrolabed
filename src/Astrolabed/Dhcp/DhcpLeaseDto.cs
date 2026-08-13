using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astrolabed.Dhcp;

internal sealed class DhcpLeaseDto
{
    [JsonPropertyName("Mac")]
    public string Mac { get; set; } = string.Empty;

    [JsonPropertyName("Ip")]
    public string Ip { get; set; } = string.Empty;

    [JsonPropertyName("ClientName")]
    public string? ClientName { get; set; }

    [JsonPropertyName("ExpiresAt")]
    public DateTimeOffset ExpiresAt { get; set; }
}
