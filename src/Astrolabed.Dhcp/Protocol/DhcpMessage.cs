using System.Net;

namespace Astrolabed.Dhcp.Protocol;

/// <summary>
/// Represents a structured RFC 2131 compliant Dynamic Host Configuration Protocol (DHCP) packet message.
/// </summary>
public class DhcpMessage
{
    /// <summary>
    /// Gets or sets the operation code indicating request or reply packet intent.
    /// </summary>
    /// <value>A <see cref="DhcpOpCode"/> enum value. Defaults to <see cref="DhcpOpCode.BootRequest"/>.</value>
    public DhcpOpCode Operation { get; set; } = DhcpOpCode.BootRequest;

    /// <summary>
    /// Gets or sets the hardware address type.
    /// </summary>
    /// <value>A byte specifying the hardware interface type (e.g., <c>1</c> for 10Mb Ethernet). Defaults to <c>1</c>.</value>
    public byte HardwareType { get; set; } = 1;

    /// <summary>
    /// Gets or sets the client hardware address length in bytes.
    /// </summary>
    /// <value>The hardware address byte length (e.g., <c>6</c> for Ethernet MAC). Defaults to <c>6</c>.</value>
    public byte HardwareAddressLength { get; set; } = 6;

    /// <summary>
    /// Gets or sets the relay agent hop count traversed by this packet.
    /// </summary>
    public byte Hops { get; set; }

    /// <summary>
    /// Gets or sets the transaction identifier (xid) chosen by the client to correlate request and response exchanges.
    /// </summary>
    public uint TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the elapsed seconds count filled in by the client since address request initiation.
    /// </summary>
    public ushort Seconds { get; set; }

    /// <summary>
    /// Gets or sets the DHCP message flags bitmask field (e.g., BROADCAST flag).
    /// </summary>
    public ushort Flags { get; set; }

    /// <summary>
    /// Gets or sets the current client IP address (ciaddr) populated by clients bound to an assigned address.
    /// </summary>
    public IPAddress ClientIpAddress { get; set; } = IPAddress.Any;

    /// <summary>
    /// Gets or sets 'your' (client) IP address (yiaddr) populated by the server offering or renewing an address.
    /// </summary>
    public IPAddress YourIpAddress { get; set; } = IPAddress.Any;

    /// <summary>
    /// Gets or sets the server IP address (siaddr) used in bootstrap phase operations.
    /// </summary>
    public IPAddress ServerIpAddress { get; set; } = IPAddress.Any;

    /// <summary>
    /// Gets or sets the relay agent IP address (giaddr) used when forwarding messages across subnets.
    /// </summary>
    public IPAddress GatewayIpAddress { get; set; } = IPAddress.Any;

    /// <summary>
    /// Gets or sets the client hardware physical address payload (chaddr).
    /// </summary>
    /// <value>A 16-byte fixed buffer containing the physical hardware address.</value>
    public byte[] ClientHardwareAddress { get; set; } = new byte[16];

    /// <summary>
    /// Gets or sets the optional server host name string (sname).
    /// </summary>
    public string ServerHostName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional boot file name string (file) used in netboot configurations.
    /// </summary>
    public string BootFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of parsed DHCP options appended to this message payload.
    /// </summary>
    public List<DhcpOption> Options { get; set; } = new();

    /// <summary>
    /// Inspects the internal options collection to retrieve the assigned <see cref="DhcpMessageType"/> option payload.
    /// </summary>
    /// <returns>The extracted <see cref="DhcpMessageType"/> enum value if present; otherwise, <c>null</c>.</returns>
    public DhcpMessageType? GetMessageType()
    {
        var option = Options.FirstOrDefault(o => o.Code == DhcpOptionCode.DhcpMessageType);
        if (option != null && option.Data.Length > 0)
        {
            return (DhcpMessageType)option.Data[0];
        }
        return null;
    }
}
