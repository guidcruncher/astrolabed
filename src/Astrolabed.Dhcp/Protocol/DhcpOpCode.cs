namespace Astrolabed.Dhcp.Protocol;

/// <summary>
/// Specifies the operation code (op) header byte indicating request or reply packet intent in DHCP/BOOTP packets.
/// </summary>
public enum DhcpOpCode : byte
{
    /// <summary>
    /// Indicates a message sent by a client requesting lease information or configuration.
    /// </summary>
    BootRequest = 1,

    /// <summary>
    /// Indicates a message sent by a server replying to a client request.
    /// </summary>
    BootReply = 2
}
