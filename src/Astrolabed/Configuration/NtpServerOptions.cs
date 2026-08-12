using System.Net;

namespace Astrolabed.Ntp;

public sealed class NtpServerOptions
{

    public bool Enabled { get; set; } = false;

    public string ListenAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 123;
    public int BufferSize { get; set; } = 65536;
    public int Stratum { get; set; } = 1;
    public string ReferenceId { get; set; } = "LOCL";

    public UpstreamNtpOptions Upstream { get; set; } = new();

    public int MaxConcurrentRequests { get; set; } = 1000;

}
