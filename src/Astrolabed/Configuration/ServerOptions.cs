using Astrolabed.Dhcp;
using Astrolabed.Dns;
using Astrolabed.Ntp;

namespace Astrolabed;

public sealed class ServerOptions
{

    public DnsForwarderOptions Dns { get; set; } = new();

    public DhcpOptions Dhcp { get; set; } = new();

    public NtpServerOptions Ntp { get; set; } = new();

    public MetricOptions Metrics { get; set; } = new();

    public WebUiOptions WebUI { get; set; } = new();

    public DbOptions DbOptions { get; set; } = new();

}
