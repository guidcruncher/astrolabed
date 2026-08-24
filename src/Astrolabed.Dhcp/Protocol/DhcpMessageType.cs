namespace Astrolabed.Dhcp.Protocol;

/// <summary>
/// Defines RFC 2131 compliant DHCP message types communicated in Option 53.
/// </summary>
public enum DhcpMessageType : byte
{
    /// <summary>
    /// Client broadcast message to discover available DHCP servers.
    /// </summary>
    Discover = 1,

    /// <summary>
    /// Server response offering IP configuration parameters to a client.
    /// </summary>
    Offer = 2,

    /// <summary>
    /// Client message requesting offered parameters or extending a lease from a specific server.
    /// </summary>
    Request = 3,

    /// <summary>
    /// Client message indicating the offered network address is already in use.
    /// </summary>
    Decline = 4,

    /// <summary>
    /// Server response acknowledging lease request and returning committed configuration parameters.
    /// </summary>
    Ack = 5,

    /// <summary>
    /// Server response refusing a client request for configuration or lease renewal.
    /// </summary>
    Nak = 6,

    /// <summary>
    /// Client message relinquishing an assigned network address and relinquishing remaining lease time.
    /// </summary>
    Release = 7,

    /// <summary>
    /// Client message requesting local configuration parameters without leasing an IP address.
    /// </summary>
    Inform = 8
}
