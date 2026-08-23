// File: src/Astrolabed.Dns/Models/DnsResponseCode.cs
namespace Astrolabed.Dns.Models;

/// <summary>
/// Represents standard DNS response codes (RCODE) as defined in RFC 1035, RFC 2136, and RFC 6891 (EDNS0).
/// </summary>
public enum DnsResponseCode : ushort
{
    /// <summary>
    /// No error condition (RCODE 0).
    /// </summary>
    NoError = 0,

    /// <summary>
    /// Format error - unable to interpret the query (RCODE 1).
    /// </summary>
    FormErr = 1,

    /// <summary>
    /// Server failure - unable to process query due to server issues (RCODE 2).
    /// </summary>
    ServFail = 2,

    /// <summary>
    /// Non-existent domain name reference (RCODE 3).
    /// </summary>
    NXDomain = 3,

    /// <summary>
    /// Not implemented - requested query type/opcode not supported (RCODE 4).
    /// </summary>
    NotImp = 4,

    /// <summary>
    /// Query refused due to policy or administrative reasons (RCODE 5).
    /// </summary>
    Refused = 5,

    /// <summary>
    /// Name exists when it should not (RFC 2136) (RCODE 6).
    /// </summary>
    YXDomain = 6,

    /// <summary>
    /// RR Set exists when it should not (RFC 2136) (RCODE 7).
    /// </summary>
    YXRRSet = 7,

    /// <summary>
    /// RR Set that should exist does not (RFC 2136) (RCODE 8).
    /// </summary>
    NXRRSet = 8,

    /// <summary>
    /// Server not authoritative for zone (RFC 2136) (RCODE 9).
    /// </summary>
    NotAuth = 9,

    /// <summary>
    /// Name not contained in zone (RFC 2136) (RCODE 10).
    /// </summary>
    NotZone = 10,

    /// <summary>
    /// DSO type not implemented (RFC 8490) (RCODE 11).
    /// </summary>
    DSOTYPENI = 11,

    /// <summary>
    /// Bad OPT version or TSIG signature failure (RFC 6891 / RFC 8945) (RCODE 16).
    /// </summary>
    BADVERS_BADSIG = 16,

    /// <summary>
    /// Key not recognized (RFC 8945) (RCODE 17).
    /// </summary>
    BADKEY = 17,

    /// <summary>
    /// Signature out of time window (RFC 8945) (RCODE 18).
    /// </summary>
    BADTIME = 18,

    /// <summary>
    /// Bad TKEY mode (RFC 2930) (RCODE 19).
    /// </summary>
    BADMODE = 19,

    /// <summary>
    /// Duplicate key name (RFC 2930) (RCODE 20).
    /// </summary>
    BADNAME = 20,

    /// <summary>
    /// Algorithm not supported (RFC 2930) (RCODE 21).
    /// </summary>
    BADALG = 21,

    /// <summary>
    /// Bad truncation (RFC 8945) (RCODE 22).
    /// </summary>
    BADTRUNC = 22,

    /// <summary>
    /// Bad/missing Server Cookie (RFC 7873) (RCODE 23).
    /// </summary>
    BADCOOKIE = 23
}
