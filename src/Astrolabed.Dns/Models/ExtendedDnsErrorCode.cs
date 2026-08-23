// File: src/Astrolabed.Dns/Models/ExtendedDnsErrorCode.cs
namespace Astrolabed.Dns.Models;

/// <summary>
/// Represents Extended DNS Error (EDE) InfoCode values as defined in RFC 8914.
/// </summary>
public enum ExtendedDnsErrorCode : ushort
{
    /// <summary>
    /// An error occurred that does not fit any other defined EDE code.
    /// </summary>
    Other = 0,

    /// <summary>
    /// The DNSKEY algorithm used to sign the zone is not supported.
    /// </summary>
    UnsupportedDnskeyAlgorithm = 1,

    /// <summary>
    /// The DS digest type used in the parent zone is not supported.
    /// </summary>
    UnsupportedDsDigestType = 2,

    /// <summary>
    /// The server returned a stale answer from cache because it was unable to reach authoritative servers.
    /// </summary>
    StaleAnswer = 3,

    /// <summary>
    /// The server detected or suspected a forged answer.
    /// </summary>
    ForgedAnswer = 4,

    /// <summary>
    /// DNSSEC validation state could not be determined.
    /// </summary>
    DnssecIndeterminate = 5,

    /// <summary>
    /// DNSSEC validation failed (the signature is bogus).
    /// </summary>
    DnssecBogus = 6,

    /// <summary>
    /// The RRSIG signature has expired.
    /// </summary>
    SignatureExpired = 7,

    /// <summary>
    /// The RRSIG signature lifetime has not started yet.
    /// </summary>
    SignatureNotYetValid = 8,

    /// <summary>
    /// The DNSKEY needed to validate the zone's RRSIG is missing.
    /// </summary>
    MissingDnskey = 9,

    /// <summary>
    /// The RRSIG record for the requested RRSet is missing.
    /// </summary>
    RrsigMissing = 10,

    /// <summary>
    /// The Zone Key bit is not set on the DNSKEY record.
    /// </summary>
    NoZoneKeyBitSet = 11,

    /// <summary>
    /// NSEC/NSEC3 record proving non-existence is missing.
    /// </summary>
    NsecMissing = 12,

    /// <summary>
    /// The server returned a cached error response.
    /// </summary>
    CachedError = 13,

    /// <summary>
    /// The server is not ready to answer queries (e.g., still initializing or loading zones).
    /// </summary>
    NotReady = 14,

    /// <summary>
    /// The request was blocked due to administrative policy or rule matching.
    /// </summary>
    Blocked = 15,

    /// <summary>
    /// The request was censored due to legal or regulatory compliance requirements.
    /// </summary>
    Censored = 16,

    /// <summary>
    /// The request was filtered by user-defined or parental control rules.
    /// </summary>
    Filtered = 17,

    /// <summary>
    /// Access to the requested record is prohibited.
    /// </summary>
    Prohibited = 18,

    /// <summary>
    /// The server returned a stale NXDOMAIN response from cache.
    /// </summary>
    StaleNxDomainAnswer = 19,

    /// <summary>
    /// The server is not authoritative for the requested domain.
    /// </summary>
    NotAuthoritative = 20,

    /// <summary>
    /// The requested feature or operation is not supported.
    /// </summary>
    NotSupported = 21,

    /// <summary>
    /// The server could not reach any authoritative servers for the zone.
    /// </summary>
    NoReachableAuthority = 22,

    /// <summary>
    /// A network failure occurred while attempting to resolve the query.
    /// </summary>
    NetworkError = 23,

    /// <summary>
    /// The server received malformed or invalid data during resolution.
    /// </summary>
    InvalidData = 24
}
