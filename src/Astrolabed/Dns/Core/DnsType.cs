namespace Astrolabed.Dns.Core;

/// <summary>
/// Represents IANA-defined DNS Resource Record (RR) TYPE codes.
/// </summary>
public enum DnsType : ushort
{
    /// <summary>
    /// A host address (IPv4). [RFC 1035]
    /// </summary>
    A = 1,

    /// <summary>
    /// An authoritative name server. [RFC 1035]
    /// </summary>
    NS = 2,

    /// <summary>
    /// A mail destination (Obsolete - use MX). [RFC 1035]
    /// </summary>
    MD = 3,

    /// <summary>
    /// A mail forwarder (Obsolete - use MX). [RFC 1035]
    /// </summary>
    MF = 4,

    /// <summary>
    /// The canonical name for an alias. [RFC 1035]
    /// </summary>
    CNAME = 5,

    /// <summary>
    /// Marks the start of a zone of authority. [RFC 1035]
    /// </summary>
    SOA = 6,

    /// <summary>
    /// A mailbox domain name (Experimental). [RFC 1035]
    /// </summary>
    MB = 7,

    /// <summary>
    /// A mail group member (Experimental). [RFC 1035]
    /// </summary>
    MG = 8,

    /// <summary>
    /// A mail rename domain name (Experimental). [RFC 1035]
    /// </summary>
    MR = 9,

    /// <summary>
    /// A null RR (Experimental). [RFC 1035]
    /// </summary>
    NULL = 10,

    /// <summary>
    /// A well known service description. [RFC 1035]
    /// </summary>
    WKS = 11,

    /// <summary>
    /// A domain name pointer. [RFC 1035]
    /// </summary>
    PTR = 12,

    /// <summary>
    /// Host information. [RFC 1035]
    /// </summary>
    HINFO = 13,

    /// <summary>
    /// Mailbox or mail list information. [RFC 1035]
    /// </summary>
    MINFO = 14,

    /// <summary>
    /// Mail exchange. [RFC 1035]
    /// </summary>
    MX = 15,

    /// <summary>
    /// Text strings. [RFC 1035]
    /// </summary>
    TXT = 16,

    /// <summary>
    /// Responsible Person. [RFC 1183]
    /// </summary>
    RP = 17,

    /// <summary>
    /// AFS Data Base location. [RFC 1183][RFC 5864]
    /// </summary>
    AFSDB = 18,

    /// <summary>
    /// X.25 PSDN address. [RFC 1183]
    /// </summary>
    X25 = 19,

    /// <summary>
    /// ISDN address. [RFC 1183]
    /// </summary>
    ISDN = 20,

    /// <summary>
    /// Route Through. [RFC 1183]
    /// </summary>
    RT = 21,

    /// <summary>
    /// NSAP address, NSAP style A record. [RFC 1706]
    /// </summary>
    NSAP = 22,

    /// <summary>
    /// Domain name pointer, NSAP style. [RFC 1348][RFC 1637][RFC 1706]
    /// </summary>
    NSAP_PTR = 23,

    /// <summary>
    /// Security signature. [RFC 2535][RFC 2931][RFC 3110][RFC 4034]
    /// </summary>
    SIG = 24,

    /// <summary>
    /// Key record. [RFC 2535][RFC 2930][RFC 3110][RFC 4034]
    /// </summary>
    KEY = 25,

    /// <summary>
    /// X.400 mail mapping information. [RFC 2163]
    /// </summary>
    PX = 26,

    /// <summary>
    /// Geographical Position. [RFC 1712]
    /// </summary>
    GPOS = 27,

    /// <summary>
    /// IP6 Address (IPv6). [RFC 3596]
    /// </summary>
    AAAA = 28,

    /// <summary>
    /// Location Information. [RFC 1876]
    /// </summary>
    LOC = 29,

    /// <summary>
    /// Next Domain (Obsolete). [RFC 2535][RFC 3755]
    /// </summary>
    NXT = 30,

    /// <summary>
    /// Endpoint Identifier.
    /// </summary>
    EID = 31,

    /// <summary>
    /// Nimrod Locator.
    /// </summary>
    NIMLOC = 32,

    /// <summary>
    /// Server Selection. [RFC 2782]
    /// </summary>
    SRV = 33,

    /// <summary>
    /// ATM Address.
    /// </summary>
    ATMA = 34,

    /// <summary>
    /// Naming Authority Pointer. [RFC 2915][RFC 2168][RFC 3403]
    /// </summary>
    NAPTR = 35,

    /// <summary>
    /// Key Exchanger. [RFC 2230]
    /// </summary>
    KX = 36,

    /// <summary>
    /// CERT. [RFC 4398]
    /// </summary>
    CERT = 37,

    /// <summary>
    /// A6 (Obsolete - use AAAA). [RFC 2874][RFC 3226][RFC 6563]
    /// </summary>
    A6 = 38,

    /// <summary>
    /// DNAME. [RFC 6672]
    /// </summary>
    DNAME = 39,

    /// <summary>
    /// Kitchen Sink.
    /// </summary>
    SINK = 40,

    /// <summary>
    /// Option record. [RFC 6891]
    /// </summary>
    OPT = 41,

    /// <summary>
    /// Address Prefix List. [RFC 3123]
    /// </summary>
    APL = 42,

    /// <summary>
    /// Delegation Signer. [RFC 4034]
    /// </summary>
    DS = 43,

    /// <summary>
    /// SSH Key Fingerprint. [RFC 4255]
    /// </summary>
    SSHFP = 44,

    /// <summary>
    /// IPsec Keying Material. [RFC 4025]
    /// </summary>
    IPSECKEY = 45,

    /// <summary>
    /// DNSSEC signature. [RFC 4034]
    /// </summary>
    RRSIG = 46,

    /// <summary>
    /// Next Secure record. [RFC 4034]
    /// </summary>
    NSEC = 47,

    /// <summary>
    /// DNS Key Record. [RFC 4034]
    /// </summary>
    DNSKEY = 48,

    /// <summary>
    /// DHCP identifier. [RFC 4701]
    /// </summary>
    DHCID = 49,

    /// <summary>
    /// NSEC record version 3. [RFC 5155]
    /// </summary>
    NSEC3 = 50,

    /// <summary>
    /// NSEC3 parameters. [RFC 5155]
    /// </summary>
    NSEC3PARAM = 51,

    /// <summary>
    /// TLSA certificate association. [RFC 6698]
    /// </summary>
    TLSA = 52,

    /// <summary>
    /// S/MIME cert association. [RFC 8162]
    /// </summary>
    SMIMEA = 53,

    /// <summary>
    /// Host Identity Protocol. [RFC 8005]
    /// </summary>
    HIP = 55,

    /// <summary>
    /// Trust Anchor LINK.
    /// </summary>
    TALINK = 58,

    /// <summary>
    /// Child DS. [RFC 7344]
    /// </summary>
    CDS = 59,

    /// <summary>
    /// Child DNSKEY. [RFC 7344]
    /// </summary>
    CDNSKEY = 60,

    /// <summary>
    /// OpenPGP Key Record. [RFC 7929]
    /// </summary>
    OPENPGPKEY = 61,

    /// <summary>
    /// Child-to-Parent Synchronization. [RFC 7477]
    /// </summary>
    CSYNC = 62,

    /// <summary>
    /// Message Digest For DNS Zones. [RFC 8976]
    /// </summary>
    ZONEMD = 63,

    /// <summary>
    /// Service Binding. [RFC 9460]
    /// </summary>
    SVCB = 64,

    /// <summary>
    /// HTTPS Binding. [RFC 9460]
    /// </summary>
    HTTPS = 65,

    /// <summary>
    /// Sender Policy Framework (Obsolete - use TXT). [RFC 7208]
    /// </summary>
    SPF = 99,

    /// <summary>
    /// Node Identifier. [RFC 6742]
    /// </summary>
    NID = 104,

    /// <summary>
    /// 32-bit IPv4 L3LOC. [RFC 6742]
    /// </summary>
    L32 = 105,

    /// <summary>
    /// 64-bit IPv6 L6LOC. [RFC 6742]
    /// </summary>
    L64 = 106,

    /// <summary>
    /// Locator Name. [RFC 6742]
    /// </summary>
    LP = 107,

    /// <summary>
    /// EUI-48 Address. [RFC 7043]
    /// </summary>
    EUI48 = 108,

    /// <summary>
    /// EUI-64 Address. [RFC 7043]
    /// </summary>
    EUI64 = 109,

    /// <summary>
    /// Transaction Key. [RFC 2930]
    /// </summary>
    TKEY = 249,

    /// <summary>
    /// Transaction Signature. [RFC 8945]
    /// </summary>
    TSIG = 250,

    /// <summary>
    /// Incremental zone transfer. [RFC 1995]
    /// </summary>
    IXFR = 251,

    /// <summary>
    /// Authoritative zone transfer. [RFC 1035][RFC 5936]
    /// </summary>
    AXFR = 252,

    /// <summary>
    /// Request for mailbox-related records (MB, MG or MR). [RFC 1035]
    /// </summary>
    MAILB = 253,

    /// <summary>
    /// Request for mail agent RRs (Obsolete - see MX). [RFC 1035]
    /// </summary>
    MAILA = 254,

    /// <summary>
    /// A request for all records (*). [RFC 1035][RFC 6895][RFC 8482]
    /// </summary>
    ANY = 255,

    /// <summary>
    /// Uniform Resource Identifier. [RFC 7553]
    /// </summary>
    URI = 256,

    /// <summary>
    /// Certification Authority Authorization. [RFC 8659]
    /// </summary>
    CAA = 257,

    /// <summary>
    /// Application Visibility and Control.
    /// </summary>
    AVC = 258,

    /// <summary>
    /// Digital Object Identifier.
    /// </summary>
    DOA = 259,

    /// <summary>
    /// DNSSEC Trust Authorities.
    /// </summary>
    TA = 32768,

    /// <summary>
    /// DNSSEC Lookaside Validation. [RFC 8749]
    /// </summary>
    DLV = 32769
}
