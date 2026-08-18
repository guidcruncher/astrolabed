using Astrolabed.Api.Services;
using Astrolabed.Dhcp;
using Astrolabed.Dns;
using Astrolabed.Ntp;
using Astrolabed.Utilities;


namespace Astrolabed;

public sealed class ServerOptions
{

    public DnsForwarderOptions Dns { get; set; } = new();

    public DhcpOptions Dhcp { get; set; } = new();

    public NtpServerOptions Ntp { get; set; } = new();

    public MetricOptions Metrics { get; set; } = new();

    public WebUiOptions WebUI { get; set; } = new();

    public DbOptions DbOptions { get; set; } = new();

    public CrossPlatformScannerOptions NetworkScanner { get; set; } = new();

    public LoggingOptions Logging { get; set; } = new();
}
