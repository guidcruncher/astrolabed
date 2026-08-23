// File: src/Astrolabed.Dns/Models/DnsType.cs
namespace Astrolabed.Dns.Models;

/// <summary>
/// Specifies the Resource Record (RR) type for DNS queries and responses as defined by IANA DNS Parameters.
/// </summary>
public enum DnsType : ushort
{
    /// <summary>
    /// Unspecified or zero value.
    /// </summary>
    None = 0,

    /// <summary>
    /// Host address (IPv4) (RFC 1035).
    /// </summary>
    A = 1,

    /// <summary>
    /// Authoritative name server (RFC 1035).
    /// </summary>
    NS = 2,

    /// <summary>
    /// Mail destination (Obsolete - use MX) (RFC 1035).
    /// </summary>
    MD = 3,

    /// <summary>
    /// Mail forwarder (Obsolete - use MX) (RFC 1035).
    /// </summary>
    MF = 4,

    /// <summary>
    /// Canonical name for an alias (RFC 1035).
    /// </summary>
    CNAME = 5,

    /// <summary>
    /// Start of a zone of authority (RFC 1035).
    /// </summary>
    SOA = 6,

    /// <summary>
    /// Mailbox domain name (Experimental) (RFC 1035).
    /// </summary>
    MB = 7,

    /// <summary>
    /// Mail group member (Experimental) (RFC 1035).
    /// </summary>
    MG = 8,

    /// <summary>
    /// Mail rename domain name (Experimental) (RFC 1035).
    /// </summary>
    MR = 9,

    /// <summary>
    /// Null RR (Experimental) (RFC 1035).
    /// </summary>
    NULL = 10,

    /// <summary>
    /// Well known service description (RFC 1035).
    /// </summary>
    WKS = 11,

    /// <summary>
    /// Domain name pointer (RFC 1035).
    /// </summary>
    PTR = 12,

    /// <summary>
    /// Host information (RFC 1035).
    /// </summary>
    HINFO = 13,

    /// <summary>
    /// Mailbox or mail list information (RFC 1035).
    /// </summary>
    MINFO = 14,

    /// <summary>
    /// Mail exchange (RFC 1035).
    /// </summary>
    MX = 15,

    /// <summary>
    /// Text strings (RFC 1035).
    /// </summary>
    TXT = 16,

    /// <summary>
    /// Responsible person (RFC 1183).
    /// </summary>
    RP = 17,

    /// <summary>
    /// AFS data base location (RFC 1183).
    /// </summary>
    AFSDB = 18,

    /// <summary>
    /// X.25 PSDN address (RFC 1183).
    /// </summary>
    X25 = 19,

    /// <summary>
    /// ISDN address (RFC 1183).
    /// </summary>
    ISDN = 20,

    /// <summary>
    /// Route through (RFC 1183).
    /// </summary>
    RT = 21,

    /// <summary>
    /// NSAP address, NSAP style A record (RFC 1706).
    /// </summary>
    NSAP = 22,

    /// <summary>
    /// Domain name pointer, NSAP style (RFC 1348).
    /// </summary>
    NSAP_PTR = 23,

    /// <summary>
    /// Security signature (RFC 2931).
    /// </summary>
    SIG = 24,

    /// <summary>
    /// Security key (RFC 2535).
    /// </summary>
    KEY = 25,

    /// <summary>
    /// X.400 mail mapping information (RFC 2163).
    /// </summary>
    PX = 26,

    /// <summary>
    /// Geographical position (RFC 1712).
    /// </summary>
    GPOS = 27,

    /// <summary>
    /// IPv6 address (RFC 3596).
    /// </summary>
    AAAA = 28,

    /// <summary>
    /// Location information (RFC 1876).
    /// </summary>
    LOC = 29,

    /// <summary>
    /// Next domain (Obsolete) (RFC 3755).
    /// </summary>
    NXT = 30,

    /// <summary>
    /// Endpoint identifier.
    /// </summary>
    EID = 31,

    /// <summary>
    /// Nimrod locator.
    /// </summary>
    NIMLOC = 32,

    /// <summary>
    /// Server selection (RFC 2782).
    /// </summary>
    SRV = 33,

    /// <summary>
    /// ATM address.
    /// </summary>
    ATMA = 34,

    /// <summary>
    /// Naming authority pointer (RFC 2915).
    /// </summary>
    NAPTR = 35,

    /// <summary>
    /// Key exchanger (RFC 2230).
    /// </summary>
    KX = 36,

    /// <summary>
    /// CERT (RFC 4398).
    /// </summary>
    CERT = 37,

    /// <summary>
    /// A6 (Obsolete - use AAAA) (RFC 3226).
    /// </summary>
    A6 = 38,

    /// <summary>
    /// DNAME (RFC 6672).
    /// </summary>
    DNAME = 39,

    /// <summary>
    /// SINK.
    /// </summary>
    SINK = 40,

    /// <summary>
    /// OPT (EDNS) (RFC 6891).
    /// </summary>
    OPT = 41,

    /// <summary>
    /// APL (RFC 3123).
    /// </summary>
    APL = 42,

    /// <summary>
    /// Delegation signer (RFC 4034).
    /// </summary>
    DS = 43,

    /// <summary>
    /// SSH key fingerprint (RFC 4255).
    /// </summary>
    SSHFP = 44,

    /// <summary>
    /// IPSECKEY (RFC 4025).
    /// </summary>
    IPSECKEY = 45,

    /// <summary>
    /// RRSIG (RFC 4034).
    /// </summary>
    RRSIG = 46,

    /// <summary>
    /// NSEC (RFC 4034).
    /// </summary>
    NSEC = 47,

    /// <summary>
    /// DNSKEY (RFC 4034).
    /// </summary>
    DNSKEY = 48,

    /// <summary>
    /// DHCID (RFC 4701).
    /// </summary>
    DHCID = 49,

    /// <summary>
    /// NSEC3 (RFC 5155).
    /// </summary>
    NSEC3 = 50,

    /// <summary>
    /// NSEC3PARAM (RFC 5155).
    /// </summary>
    NSEC3PARAM = 51,

    /// <summary>
    /// TLSA (RFC 6698).
    /// </summary>
    TLSA = 52,

    /// <summary>
    /// S/MIME cert association (RFC 8162).
    /// </summary>
    SMIMEA = 53,

    /// <summary>
    /// Host Identity Protocol (RFC 8005).
    /// </summary>
    HIP = 55,

    /// <summary>
    /// NINFO.
    /// </summary>
    NINFO = 56,

    /// <summary>
    /// RKEY.
    /// </summary>
    RKEY = 57,

    /// <summary>
    /// Trust Anchor LINK.
    /// </summary>
    TALINK = 58,

    /// <summary>
    /// Child DS (RFC 7344).
    /// </summary>
    CDS = 59,

    /// <summary>
    /// DNSKEY(s) the Child wants reflected in DS (RFC 7344).
    /// </summary>
    CDNSKEY = 60,

    /// <summary>
    /// OpenPGP Key (RFC 7929).
    /// </summary>
    OPENPGPKEY = 61,

    /// <summary>
    /// Child-To-Parent Synchronization (RFC 7477).
    /// </summary>
    CSYNC = 62,

    /// <summary>
    /// Message Digest for DNS Zones (RFC 8976).
    /// </summary>
    ZONEMD = 63,

    /// <summary>
    /// Service Binding (RFC 9460).
    /// </summary>
    SVCB = 64,

    /// <summary>
    /// HTTPS Binding (RFC 9460).
    /// </summary>
    HTTPS = 65,

    /// <summary>
    /// Sender Policy Framework (Obsolete - use TXT) (RFC 7208).
    /// </summary>
    SPF = 99,

    /// <summary>
    /// UINFO.
    /// </summary>
    UINFO = 100,

    /// <summary>
    /// UID.
    /// </summary>
    UID = 101,

    /// <summary>
    /// GID.
    /// </summary>
    GID = 102,

    /// <summary>
    /// UNSPEC.
    /// </summary>
    UNSPEC = 103,

    /// <summary>
    /// NINFO2 (Node Identifier).
    /// </summary>
    NINFO2 = 104,

    /// <summary>
    /// UI.
    /// </summary>
    UI = 105,

    /// <summary>
    /// Transaction Key (RFC 2930).
    /// </summary>
    TKEY = 249,

    /// <summary>
    /// Transaction Signature (RFC 2845).
    /// </summary>
    TSIG = 250,

    /// <summary>
    /// Incremental zone transfer (RFC 1995).
    /// </summary>
    IXFR = 251,

    /// <summary>
    /// Transfer of an entire zone (RFC 1035).
    /// </summary>
    AXFR = 252,

    /// <summary>
    /// Mailbox-related records (MB, MG or MR) (RFC 1035).
    /// </summary>
    MAILB = 253,

    /// <summary>
    /// Mail agent RRs (Obsolete - use MX) (RFC 1035).
    /// </summary>
    MAILA = 254,

    /// <summary>
    /// A request for all records (*).
    /// </summary>
    ANY = 255,

    /// <summary>
    /// URI (RFC 7553).
    /// </summary>
    URI = 256,

    /// <summary>
    /// Certification Authority Authorization (RFC 8659).
    /// </summary>
    CAA = 257,

    /// <summary>
    /// Application Visibility and Control.
    /// </summary>
    AVC = 258,

    /// <summary>
    /// Digital Object Architecture.
    /// </summary>
    DOA = 259,

    /// <summary>
    /// Automatic Multicast Tunneling Relay (RFC 8777).
    /// </summary>
    AMTRELAY = 260,

    /// <summary>
    /// DNSSEC Trust Authorities.
    /// </summary>
    TA = 32768,

    /// <summary>
    /// DNSSEC Lookaside Validation (RFC 4431).
    /// </summary>
    DLV = 32769
}
