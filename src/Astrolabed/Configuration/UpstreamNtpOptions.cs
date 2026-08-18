using System.Net;

namespace Astrolabed.Ntp;

public sealed class UpstreamNtpOptions
{

    public const string SectionName = "Ntp:Upstream";

    public bool Enabled { get; set; } = true;

    public string[] Servers { get; set; } =
    [
    ];

    public int PollIntervalSeconds { get; set; } = 64;
}
