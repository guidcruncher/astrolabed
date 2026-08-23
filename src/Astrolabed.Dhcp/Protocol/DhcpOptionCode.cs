namespace Astrolabed.Dhcp.Protocol;

/// <summary>
/// Defines RFC-standard DHCP Option Code tags used to request and transmit configuration parameters.
/// </summary>
public enum DhcpOptionCode : byte
{
    /// <summary>
    /// Option code 0: Pad option used to align field boundaries.
    /// </summary>
    Pad = 0,

    /// <summary>
    /// Option code 1: Subnet mask of the client interface.
    /// </summary>
    SubnetMask = 1,

    /// <summary>
    /// Option code 3: List of IP addresses for routers on the client's subnet.
    /// </summary>
    Router = 3,

    /// <summary>
    /// Option code 6: List of IP addresses for DNS name servers available to the client.
    /// </summary>
    DnsServer = 6,

    /// <summary>
    /// Option code 12: Name of the client host.
    /// </summary>
    HostName = 12,

    /// <summary>
    /// Option code 15: Domain name the client should use when resolving hostnames.
    /// </summary>
    DomainName = 15,

    /// <summary>
    /// Option code 42: List of IP addresses for NTP servers available to the client.
    /// </summary>
    NtpServer = 42,

    /// <summary>
    /// Option code 50: Explicitly requested IP address sent by the client.
    /// </summary>
    RequestedIpAddress = 50,

    /// <summary>
    /// Option code 51: Lease time duration in seconds offered or assigned to the client.
    /// </summary>
    AddressLeaseTime = 51,

    /// <summary>
    /// Option code 52: Indicates whether 'file' or 'sname' fields carry option fields.
    /// </summary>
    OptionOverload = 52,

    /// <summary>
    /// Option code 53: Expresses the specific <see cref="DhcpMessageType"/> of the packet.
    /// </summary>
    DhcpMessageType = 53,

    /// <summary>
    /// Option code 54: IP address server identifier.
    /// </summary>
    ServerIdentifier = 54,

    /// <summary>
    /// Option code 55: List of requested option codes submitted by the client.
    /// </summary>
    ParameterRequestList = 55,

    /// <summary>
    /// Option code 56: Error or informational message string payload sent by the server.
    /// </summary>
    Message = 56,

    /// <summary>
    /// Option code 57: Maximum size in bytes of a DHCP message the client can reassemble.
    /// </summary>
    MaximumDhcpMessageSize = 57,

    /// <summary>
    /// Option code 58: T1 renewal time value in seconds.
    /// </summary>
    RenewalTimeValue = 58,

    /// <summary>
    /// Option code 59: T2 rebinding time value in seconds.
    /// </summary>
    RebindingTimeValue = 59,

    /// <summary>
    /// Option code 61: Unique identifier specifying the client to the server.
    /// </summary>
    ClientIdentifier = 61,

    /// <summary>
    /// Option code 255: End option indicating the end of valid options in a vendor field.
    /// </summary>
    End = 255
}
