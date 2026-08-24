namespace Astrolabed.Ntp.Options;

/// <summary>
/// Specifies the source mode used by the NTP server to resolve current network time.
/// </summary>
public enum TimeResolverMode
{
    /// <summary>
    /// Time is resolved using the host system's local clock.
    /// </summary>
    Local,

    /// <summary>
    /// Time is resolved by querying remote upstream NTP servers.
    /// </summary>
    Upstream
}
