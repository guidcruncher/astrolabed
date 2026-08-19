namespace Astrolabed.Dns.Core;

/// <summary>
/// Represents Extended DNS Error (EDE) codes carried in EDNS OPT records 
/// as defined in RFC 8914 and the IANA Extended DNS Error Codes Registry.
/// </summary>
public enum ExtendedDnsErrorCode : ushort
{
    /// <summary>
    /// Other Error - The error does not fit into any other defined category (RFC 8914).
    /// </summary>
    Other = 0,

    /// <summary>
    /// Unsupported DNSKEY Algorithm - The zone is signed with an unsupported algorithm (RFC 8914).
    /// </summary>
    UnsupportedDnskeyAlgorithm = 1,

    /// <summary>
    /// Unsupported DS Digest Type - The zone uses an unsupported DS digest algorithm (RFC 8914).
    /// </summary>
    UnsupportedDsDigestType = 2,

    /// <summary>
    /// Stale Answer - The server returned a stale answer from cache (RFC 8914).
    /// </summary>
    StaleAnswer = 3,

    /// <summary>
    /// Forged Answer - The answer was forged or substituted for policy reasons (RFC 8914).
    /// </summary>
    ForgedAnswer = 4,

    /// <summary>
    /// DNSSEC Indeterminate - The resolver was unable to determine validation status (RFC 8914).
    /// </summary>
    DnssecIndeterminate = 5,

    /// <summary>
    /// DNSSEC Bogus - The response failed DNSSEC validation (RFC 8914).
    /// </summary>
    DnssecBogus = 6,

    /// <summary>
    /// Signature Expired - RRSIG failed validation because it expired (RFC 8914).
    /// </summary>
    SignatureExpired = 7,

    /// <summary>
    /// Signature Not Yet Valid - RRSIG failed validation because inception date is in the future (RFC 8914).
    /// </summary>
    SignatureNotYetValid = 8,

    /// <summary>
    /// DNSKEY Missing - Required DNSKEY RR is missing from the zone (RFC 8914).
    /// </summary>
    DnskeyMissing = 9,

    /// <summary>
    /// RRSIGs Missing - Expected RRSIG records are missing (RFC 8914).
    /// </summary>
    RrsigsMissing = 10,

    /// <summary>
    /// No Zone Key Bit Set - DNSKEY record found but Zone Key bit was not set (RFC 8914).
    /// </summary>
    NoZoneKeyBitSet = 11,

    /// <summary>
    /// NSEC Missing - Expected NSEC/NSEC3 proof of non-existence is missing (RFC 8914).
    /// </summary>
    NsecMissing = 12,

    /// <summary>
    /// Cached Error - Information cached from a upstream lookup resulted in error (RFC 8914).
    /// </summary>
    CachedError = 13,

    /// <summary>
    /// Not Ready - Server is not yet ready to serve requests (RFC 8914).
    /// </summary>
    NotReady = 14,

    /// <summary>
    /// Blocked - Request was blocked by administrative policy or blocklist (RFC 8914).
    /// </summary>
    Blocked = 15,

    /// <summary>
    /// Censored - Request was blocked due to legal or regulatory requirements (RFC 8914).
    /// </summary>
    Censored = 16,

    /// <summary>
    /// Filtered - Request was filtered according to user-configured settings (RFC 8914).
    /// </summary>
    Filtered = 17,

    /// <summary>
    /// Prohibited - Request was prohibited by authoritative server policy (RFC 8914).
    /// </summary>
    Prohibited = 18,

    /// <summary>
    /// Stale NXDOMAIN Answer - Cached NXDOMAIN returned when upstream is unreachable (RFC 8914).
    /// </summary>
    StaleNxDomainAnswer = 19,

    /// <summary>
    /// Not Authoritative - Server is not authoritative for the zone requested (RFC 8914).
    /// </summary>
    NotAuthoritative = 20,

    /// <summary>
    /// Not Supported - Operation, opcode, or feature is not supported (RFC 8914).
    /// </summary>
    NotSupported = 21,

    /// <summary>
    /// No Reachable Authority - Server could not contact any authoritative server (RFC 8914).
    /// </summary>
    NoReachableAuthority = 22,

    /// <summary>
    /// Network Error - Network-level failure occurred during upstream resolution (RFC 8914).
    /// </summary>
    NetworkError = 23,

    /// <summary>
    /// Invalid Data - Server received invalid data from upstream server (RFC 8914).
    /// </summary>
    InvalidData = 24,

    /// <summary>
    /// Signature Expired Before Inception - RRSIG expiration predates inception (RFC 8914).
    /// </summary>
    SignatureExpiredBeforeInception = 25,

    /// <summary>
    /// Too Many Records - Zone or response contained too many records to process (RFC 8914).
    /// </summary>
    TooManyRecords = 26,

    /// <summary>
    /// Unsupported AAAA Guard - Response omitted AAAA due to guard mechanism (RFC 8914).
    /// </summary>
    UnsupportedAaaaGuard = 27
}


/// <summary>
/// Helper extensions for formatting and handling <see cref="ExtendedDnsErrorCode"/>.
/// </summary>
public static class ExtendedDnsErrorCodeExtensions
{
    /// <summary>
    /// Returns the standard IANA text representation of the Extended DNS Error code.
    /// </summary>
    public static string ToCanonicalName(this ExtendedDnsErrorCode edeCode) => edeCode switch
    {
        ExtendedDnsErrorCode.Other => "Other Error",
        ExtendedDnsErrorCode.UnsupportedDnskeyAlgorithm => "Unsupported DNSKEY Algorithm",
        ExtendedDnsErrorCode.UnsupportedDsDigestType => "Unsupported DS Digest Type",
        ExtendedDnsErrorCode.StaleAnswer => "Stale Answer",
        ExtendedDnsErrorCode.ForgedAnswer => "Forged Answer",
        ExtendedDnsErrorCode.DnssecIndeterminate => "DNSSEC Indeterminate",
        ExtendedDnsErrorCode.DnssecBogus => "DNSSEC Bogus",
        ExtendedDnsErrorCode.SignatureExpired => "Signature Expired",
        ExtendedDnsErrorCode.SignatureNotYetValid => "Signature Not Yet Valid",
        ExtendedDnsErrorCode.DnskeyMissing => "DNSKEY Missing",
        ExtendedDnsErrorCode.RrsigsMissing => "RRSIGs Missing",
        ExtendedDnsErrorCode.NoZoneKeyBitSet => "No Zone Key Bit Set",
        ExtendedDnsErrorCode.NsecMissing => "NSEC Missing",
        ExtendedDnsErrorCode.CachedError => "Cached Error",
        ExtendedDnsErrorCode.NotReady => "Not Ready",
        ExtendedDnsErrorCode.Blocked => "Blocked",
        ExtendedDnsErrorCode.Censored => "Censored",
        ExtendedDnsErrorCode.Filtered => "Filtered",
        ExtendedDnsErrorCode.Prohibited => "Prohibited",
        ExtendedDnsErrorCode.StaleNxDomainAnswer => "Stale NXDOMAIN Answer",
        ExtendedDnsErrorCode.NotAuthoritative => "Not Authoritative",
        ExtendedDnsErrorCode.NotSupported => "Not Supported",
        ExtendedDnsErrorCode.NoReachableAuthority => "No Reachable Authority",
        ExtendedDnsErrorCode.NetworkError => "Network Error",
        ExtendedDnsErrorCode.InvalidData => "Invalid Data",
        ExtendedDnsErrorCode.SignatureExpiredBeforeInception => "Signature Expired Before Inception",
        ExtendedDnsErrorCode.TooManyRecords => "Too Many Records",
        ExtendedDnsErrorCode.UnsupportedAaaaGuard => "Unsupported AAAA Guard",
        _ => $"EDE_{(ushort)edeCode}"
    };
}
