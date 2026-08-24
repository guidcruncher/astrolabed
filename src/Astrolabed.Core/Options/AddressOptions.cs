// File: src/Astrolabed.Dns/Options/AddressOptions.cs
namespace Astrolabed.Core.Options;

/// <summary>
/// Represents network endpoint configuration options for network listeners and protocol services.
/// </summary>
public sealed class AddressOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the network endpoint listener is enabled.
    /// </summary>
    /// <value><c>true</c> if enabled; otherwise, <c>false</c>. Defaults to <c>true</c>.</value>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the IP address or host network interface to bind to.
    /// </summary>
    /// <value>A string representation of an IP address (e.g., "0.0.0.0" or "127.0.0.1"). Defaults to an empty string.</value>
    public string Address { get; set; } = "";

    /// <summary>
    /// Gets or sets the target transport layer network port.
    /// </summary>
    /// <value>The port number to listen on. Defaults to <c>53</c> (standard DNS port).</value>
    public int Port { get; set; } = 53;
}
