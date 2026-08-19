// File: src/Astrolabed.Dns/Models/DnsWireMessage.cs
using System.Collections.Generic;

namespace Astrolabed.Dns.Models;

public sealed class DnsWireMessage
{
    public ushort TransactionId { get; set; }
    public bool IsResponse { get; set; }
    public DnsOpCode OpCode { get; set; }
    public bool AuthoritativeAnswer { get; set; }
    public bool Truncated { get; set; }
    public bool RecursionDesired { get; set; }
    public bool RecursionAvailable { get; set; }
    public DnsResponseCode ResponseCode { get; set; }

    public string QuestionName { get; set; } = string.Empty;
    public DnsType QuestionType { get; set; }
    public ushort QuestionClass { get; set; } = 1;

    public List<DnsResourceRecord> Answers { get; } = new();
    public List<DnsResourceRecord> Authorities { get; } = new();
    public List<DnsResourceRecord> Additionals { get; } = new();

    public EdnsOptions? Edns { get; set; }
}
