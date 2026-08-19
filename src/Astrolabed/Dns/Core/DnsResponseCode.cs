namespace Astrolabed.Dns.Core;

/// <summary>
/// Represents standard Domain Name System (DNS) Response Codes (RCODEs) 
/// as defined in RFC 1035, RFC 2136, RFC 2671, RFC 6891, and related specifications.
/// </summary>
public enum DnsResponseCode : ushort
{
    /// <summary>
    /// No error condition (RFC 1035).
    /// </summary>
    NoError = 0,

    /// <summary>
    /// Format error - The name server was unable to interpret the query (RFC 1035).
    /// </summary>
    FormatError = 1,

    /// <summary>
    /// Server failure - The name server was unable to process this query due to a problem with the name server (RFC 1035).
    /// </summary>
    ServerFailure = 2,

    /// <summary>
    /// Name Error - Meaningful only for responses from an authoritative name server, this code signifies that the domain name referenced in the query does not exist (RFC 1035).
    /// </summary>
    NonExistentDomain = 3,

    /// <summary>
    /// Not Implemented - The name server does not support the requested kind of query (RFC 1035).
    /// </summary>
    NotImplemented = 4,

    /// <summary>
    /// Refused - The name server refuses to perform the specified operation for policy reasons (RFC 1035).
    /// </summary>
    Refused = 5,

    /// <summary>
    /// YXDomain - Name Exists when it should not (RFC 2136).
    /// </summary>
    NameExists = 6,

    /// <summary>
    /// YXRRSet - RR Set Exists when it should not (RFC 2136).
    /// </summary>
    RRSetExists = 7,

    /// <summary>
    /// NXRRSet - RR Set that should exist does not (RFC 2136).
    /// </summary>
    RRSetDoesNotExist = 8,

    /// <summary>
    /// NotAuth - Server Not Authoritative for zone (RFC 2136) or Not Authorized (RFC 2845).
    /// </summary>
    NotAuthoritative = 9,

    /// <summary>
    /// NotZone - Name not contained in zone (RFC 2136).
    /// </summary>
    NameNotInZone = 10,

    /// <summary>
    /// DSO-TYPE Not Implemented (RFC 8490).
    /// </summary>
    DsoTypeNotImplemented = 11,

    /// <summary>
    /// Bad OPT Version (RFC 6891) or TSIG Signature Failure (RFC 2845).
    /// </summary>
    BadVersionOrBadSig = 16,

    /// <summary>
    /// Key not recognized (RFC 2845).
    /// </summary>
    BadKey = 17,

    /// <summary>
    /// Signature out of time window (RFC 2845).
    /// </summary>
    BadTime = 18,

    /// <summary>
    /// Bad TKEY Mode (RFC 2930).
    /// </summary>
    BadMode = 19,

    /// <summary>
    /// Duplicate key name (RFC 2930).
    /// </summary>
    BadName = 20,

    /// <summary>
    /// Algorithm not supported (RFC 2930).
    /// </summary>
    BadAlgorithm = 21,

    /// <summary>
    /// Bad Truncation (RFC 4635).
    /// </summary>
    BadTruncation = 22,

    /// <summary>
    /// Bad/missing Server Cookie (RFC 7873).
    /// </summary>
    BadCookie = 23
}

/// <summary>
/// Provides string constants and extension methods for standard DNS Response Code (RCODE) mnemonics.
/// </summary>
public static class DnsResponseCodeExtensions
{
    public const string NoError = "NOERROR";
    public const string FormErr = "FORMERR";
    public const string ServFail = "SERVFAIL";
    public const string NxDomain = "NXDOMAIN";
    public const string NotImp = "NOTIMP";
    public const string Refused = "REFUSED";
    public const string YxDomain = "YXDOMAIN";
    public const string YxRRSet = "YXRRSET";
    public const string NxRRSet = "NXRRSET";
    public const string NotAuth = "NOTAUTH";
    public const string NotZone = "NOTZONE";
    public const string DsoTypeNotImp = "DSOTYPENOTIMP";
    public const string BadVersOrBadSig = "BADVERS";
    public const string BadKey = "BADKEY";
    public const string BadTime = "BADTIME";
    public const string BadMode = "BADMODE";
    public const string BadName = "BADNAME";
    public const string BadAlg = "BADALG";
    public const string BadTrunc = "BADTRUNC";
    public const string BadCookie = "BADCOOKIE";

    /// <summary>
    /// Converts a <see cref="DnsResponseCode"/> enum to its standard RFC string mnemonic representation.
    /// </summary>
    public static string ToMnemonic(this DnsResponseCode rcode) => rcode switch
    {
        DnsResponseCode.NoError => NoError,
        DnsResponseCode.FormatError => FormErr,
        DnsResponseCode.ServerFailure => ServFail,
        DnsResponseCode.NonExistentDomain => NxDomain,
        DnsResponseCode.NotImplemented => NotImp,
        DnsResponseCode.Refused => Refused,
        DnsResponseCode.NameExists => YxDomain,
        DnsResponseCode.RRSetExists => YxRRSet,
        DnsResponseCode.RRSetDoesNotExist => NxRRSet,
        DnsResponseCode.NotAuthoritative => NotAuth,
        DnsResponseCode.NameNotInZone => NotZone,
        DnsResponseCode.DsoTypeNotImplemented => DsoTypeNotImp,
        DnsResponseCode.BadVersionOrBadSig => BadVersOrBadSig,
        DnsResponseCode.BadKey => BadKey,
        DnsResponseCode.BadTime => BadTime,
        DnsResponseCode.BadMode => BadMode,
        DnsResponseCode.BadName => BadName,
        DnsResponseCode.BadAlgorithm => BadAlg,
        DnsResponseCode.BadTruncation => BadTrunc,
        DnsResponseCode.BadCookie => BadCookie,
        _ => $"RCODE_{(ushort)rcode}"
    };

    /// <summary>
    /// Parses a string mnemonic (e.g., "NXDOMAIN", "SERVFAIL") into its corresponding <see cref="DnsResponseCode"/>.
    /// </summary>
    public static bool TryParseMnemonic(ReadOnlySpan<char> mnemonic, out DnsResponseCode rcode)
    {
        if (mnemonic.Equals(NoError, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.NoError; return true; }
        if (mnemonic.Equals(FormErr, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.FormatError; return true; }
        if (mnemonic.Equals(ServFail, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.ServerFailure; return true; }
        if (mnemonic.Equals(NxDomain, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.NonExistentDomain; return true; }
        if (mnemonic.Equals(NotImp, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.NotImplemented; return true; }
        if (mnemonic.Equals(Refused, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.Refused; return true; }
        if (mnemonic.Equals(YxDomain, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.NameExists; return true; }
        if (mnemonic.Equals(YxRRSet, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.RRSetExists; return true; }
        if (mnemonic.Equals(NxRRSet, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.RRSetDoesNotExist; return true; }
        if (mnemonic.Equals(NotAuth, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.NotAuthoritative; return true; }
        if (mnemonic.Equals(NotZone, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.NameNotInZone; return true; }
        if (mnemonic.Equals(DsoTypeNotImp, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.DsoTypeNotImplemented; return true; }
        if (mnemonic.Equals(BadVersOrBadSig, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.BadVersionOrBadSig; return true; }
        if (mnemonic.Equals(BadKey, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.BadKey; return true; }
        if (mnemonic.Equals(BadTime, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.BadTime; return true; }
        if (mnemonic.Equals(BadMode, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.BadMode; return true; }
        if (mnemonic.Equals(BadName, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.BadName; return true; }
        if (mnemonic.Equals(BadAlg, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.BadAlgorithm; return true; }
        if (mnemonic.Equals(BadTrunc, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.BadTruncation; return true; }
        if (mnemonic.Equals(BadCookie, StringComparison.OrdinalIgnoreCase)) { rcode = DnsResponseCode.BadCookie; return true; }

        rcode = default;
        return false;
    }
}
