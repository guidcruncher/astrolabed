// File: src/Astrolabed.Dns/Options/AddressOptions.cs
namespace Astrolabed.Dns.Options;

public sealed class AddressOptions
{
    public bool Enabled { get; set; } = true;
    public string Address { get; set; } = "";
    public int Port { get; set; } = 53;
}
