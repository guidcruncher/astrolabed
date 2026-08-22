// File: src/Astrolabed.Dns/Models/DnsWireMessage.cs
namespace Astrolabed.Dns.Models;

/// <summary>
/// Represents a parsed or outgoing DNS wire format message.
/// </summary>
public sealed class DnsWireMessage
{
    /// <summary>
    /// Gets or sets the 16-bit transaction identifier.
    /// </summary>
    public ushort TransactionId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this message is a response.
    /// </summary>
    public bool IsResponse { get; set; }

    /// <summary>
    /// Gets or sets the DNS OpCode.
    /// </summary>
    public DnsOpCode OpCode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the responding server is an authority for the domain.
    /// </summary>
    public bool AuthoritativeAnswer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the message was truncated due to length limits.
    /// </summary>
    public bool Truncated { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether recursion is desired.
    /// </summary>
    public bool RecursionDesired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether recursion is available on the server.
    /// </summary>
    public bool RecursionAvailable { get; set; }

    /// <summary>
    /// Gets or sets the overall response code (RCODE), including any upper bits from EDNS.
    /// </summary>
    public DnsResponseCode ResponseCode { get; set; }

    /// <summary>
    /// Gets or sets the domain name being queried.
    /// </summary>
    public string QuestionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the query type (e.g., A, AAAA, MX, OPT).
    /// </summary>
    public DnsType QuestionType { get; set; }

    /// <summary>
    /// Gets or sets the query class (typically 1 for IN/Internet).
    /// </summary>
    public ushort QuestionClass { get; set; } = 1;

    /// <summary>
    /// Gets the list of resource records in the Answer section.
    /// </summary>
    public List<DnsResourceRecord> Answers { get; } = [];

    /// <summary>
    /// Gets the list of resource records in the Authority section.
    /// </summary>
    public List<DnsResourceRecord> Authorities { get; } = [];

    /// <summary>
    /// Gets the list of resource records in the Additional section.
    /// </summary>
    public List<DnsResourceRecord> Additionals { get; } = [];

    /// <summary>
    /// Gets or sets the EDNS options associated with this message, if present.
    /// </summary>
    public EdnsOptions? Edns { get; set; }
}
