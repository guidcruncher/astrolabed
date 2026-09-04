namespace Astrolabed.Core.Options;

/// <summary>
/// Defines configuration options for the <see cref="Network.PingService"/>.
/// </summary>
public class PingServiceOptions
{
    /// <summary>
    /// The default configuration section name used to bind these options.
    /// </summary>
    public const string SectionName = "PingService";

    /// <summary>
    /// Gets or sets the maximum time, in milliseconds, to wait for a ping response.
    /// </summary>
    /// <value>The default value is <c>1000</c> milliseconds.</value>
    public int TimeoutMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the Time-to-Live (TTL) value that specifies the maximum number of router hops 
    /// a packet can traverse before being discarded.
    /// </summary>
    /// <value>The default value is <c>64</c>.</value>
    public int Ttl { get; set; } = 64;

    /// <summary>
    /// Gets or sets a value indicating whether data sent to the remote host can be fragmented across multiple packets.
    /// </summary>
    /// <value><see langword="true"/> if data cannot be fragmented; otherwise, <see langword="false"/>. The default value is <see langword="true"/>.</value>
    public bool DontFragment { get; set; } = true;
}

