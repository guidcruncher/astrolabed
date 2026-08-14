using System.Net;

namespace Astrolabed.Ntp;

public sealed class MetricOptions
{
    public const string SectionName = "Metrics";

    public bool Enabled { get; set; } = false;

    public string StorageEngine { get; set; } = "";

    public string Location { get; set; } = "";

    public string ListenAddress { get; set; } = "0.0.0.0";
    public int ListenPort { get; set; } = 1080;

}
