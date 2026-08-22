// File: src/Astrolabed.Dns/Models/ExtendedDnsError.cs
namespace Astrolabed.Dns.Models;

public sealed class ExtendedDnsError
{
    public ExtendedDnsErrorCode InfoCode { get; set; }
    public string ExtraText { get; set; } = string.Empty;
}
