// File: src/Astrolabed.Dns/Models/ExtendedDnsErrorCode.cs
namespace Astrolabed.Dns.Models;

public enum ExtendedDnsErrorCode : ushort
{
    Other = 0,
    UnsupportedDnskeyAlgorithm = 1,
    UnsupportedDsDigestType = 2,
    StaleAnswer = 3,
    ForgedAnswer = 4,
    DnssecIndeterminate = 5,
    DnssecBogus = 6,
    SignatureExpired = 7,
    SignatureNotYetValid = 8,
    MissingDnskey = 9,
    RrsigMissing = 10,
    NoZoneKeyBitSet = 11,
    NsecMissing = 12,
    CachedError = 13,
    NotReady = 14,
    Blocked = 15,
    Censored = 16,
    Filtered = 17,
    Prohibited = 18,
    StaleNxDomainAnswer = 19,
    NotAuthoritative = 20,
    NotSupported = 21,
    NoReachableAuthority = 22,
    NetworkError = 23,
    InvalidData = 24
}
