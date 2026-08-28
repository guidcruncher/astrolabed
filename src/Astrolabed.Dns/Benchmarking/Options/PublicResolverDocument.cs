// File: PublicResolversDocument.cs
namespace Astrolabed.Dns.Benchmarking.Options;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the root object structure of the public-resolvers.json file.
/// </summary>
public sealed class PublicResolversDocument
{
    /// <summary>
    /// Gets or sets the collection of public DNS server configurations parsed from JSON.
    /// </summary>
    [JsonPropertyName("public_dns_servers")]
    public List<DnsServerConfig> PublicDnsServers { get; set; } = [];
}
