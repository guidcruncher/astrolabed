namespace QuickPing.Configuration;

public class PingServiceOptions
{
    public const string SectionName = "PingService";

    public int TimeoutMilliseconds { get; set; } = 1000;
    public int Ttl { get; set; } = 64;
    public bool DontFragment { get; set; } = true;
}
