using System.Net;

namespace Astrolabed.Ntp;

public sealed class NtpServerOptions
{
    public const string SectionName = "Ntp";

    public bool Enabled { get; set; } = false;

    public string ListenAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 123;
    public int BufferSize { get; set; } = 65536;

    public UpstreamNtpOptions Upstream { get; set; } = new();

    public int MaxConcurrentRequests { get; set; } = 1000;

}
