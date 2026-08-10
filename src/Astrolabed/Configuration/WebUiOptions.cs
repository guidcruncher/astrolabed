using System.Net;

namespace Astrolabed.Ntp;

public sealed class WebUiOptions
{

    public bool Enabled { get; set; } = false;

    public string ListenAddress { get; set; } = "0.0.0.0";

    public int ListenPort { get; set; } = 1081;

}
