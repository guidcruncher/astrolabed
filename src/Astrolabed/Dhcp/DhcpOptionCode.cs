namespace Astrolabed.Dhcp;

/// <summary>
/// Represents standard DHCP Option Codes defined by IANA (RFC 2132 and related RFCs).
/// </summary>
public enum DhcpOptionCode : byte
{
    // RFC 2132 - Padding and End
    Pad = 0,
    SubnetMask = 1,
    TimeOffset = 2,
    Router = 3,
    TimeServer = 4,
    NameServer = 5,
    DomainNameServer = 6,
    LogServer = 7,
    CookieServer = 8,
    LprServer = 9,
    ImpressServer = 10,
    ResourceLocationServer = 11,
    HostName = 12,
    BootFileSize = 13,
    MeritDumpFile = 14,
    DomainName = 15,
    SwapServer = 16,
    RootPath = 17,
    ExtensionsPath = 18,

    // IP Layer Parameters per Interface
    IpForwarding = 19,
    NonLocalSourceRouting = 20,
    PolicyFilter = 21,
    MaximumDatagramReassemblySize = 22,
    DefaultIpTimeToLive = 23,
    PathMtuAgingTimeout = 24,
    PathMtuPlateauTable = 25,

    // IP Layer Parameters per Host
    InterfaceMtu = 26,
    AllSubnetsAreLocal = 27,
    BroadcastAddress = 28,
    PerformMaskDiscovery = 29,
    MaskSupplier = 30,
    PerformRouterDiscovery = 31,
    RouterSolicitationAddress = 32,
    StaticRoute = 33,

    // Link Layer Parameters per Interface
    TrailerEncapsulation = 34,
    ArpCacheTimeout = 35,
    EthernetEncapsulation = 36,

    // TCP Parameters
    TcpDefaultTtl = 37,
    TcpKeepaliveInterval = 38,
    TcpKeepaliveGarbage = 39,

    // Application and Service Parameters
    NetworkInformationServiceDomain = 40,
    NetworkInformationServers = 41,
    NetworkTimeProtocolServers = 42,
    VendorSpecificInformation = 43,
    NetBiosOverTcpIpNameServer = 44,
    NetBiosOverTcpIpDatagramDistributionServer = 45,
    NetBiosOverTcpIpNodeType = 46,
    NetBiosOverTcpIpScope = 47,
    XWindowSystemFontServer = 48,
    XWindowSystemDisplayManager = 49,

    // DHCP Extensions
    RequestedIpAddress = 50,
    IpAddressLeaseTime = 51,
    OptionOverload = 52,
    DhcpMessageType = 53,
    DhcpServerIdentifier = 54,
    ParameterRequestList = 55,
    Message = 56,
    MaximumDhcpMessageSize = 57,
    RenewalTimeValue = 58,
    RebindingTimeValue = 59,
    VendorClassIdentifier = 60,
    ClientIdentifier = 61,

    // Network Boot / PXE
    TftpServerName = 66,
    BootfileName = 67,

    // Additional Common RFC Extensions
    ClientFqdn = 81,               // RFC 4702
    RelayAgentInformation = 82,    // RFC 3046
    ClasslessStaticRoute = 121,    // RFC 3442
    WebProxyAutoDiscovery = 252,   // WPAD

    // RFC 2132 - End Marker
    End = 255
}
