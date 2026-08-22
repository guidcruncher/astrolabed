namespace Astrolabed.Dhcp.Protocol;

public enum DhcpOptionCode : byte
{
    Pad = 0,
    SubnetMask = 1,
    Router = 3,
    DnsServer = 6,
    HostName = 12,
    DomainName = 15,
    RequestedIpAddress = 50,
    AddressLeaseTime = 51,
    OptionOverload = 52,
    DhcpMessageType = 53,
    ServerIdentifier = 54,
    ParameterRequestList = 55,
    Message = 56,
    MaximumDhcpMessageSize = 57,
    RenewalTimeValue = 58,
    RebindingTimeValue = 59,
    ClientIdentifier = 61,
    End = 255
}
