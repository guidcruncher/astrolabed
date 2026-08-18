using System;
using System.Buffers.Binary;

using Astrolabed.Dns.Core;

namespace Astrolabed.Dns.Core;

public readonly struct DnsRequestContext
{
    public string ClientIp { get; }
    public string ClientName { get; }
    public string Domain { get; }
    public ushort TransactionId { get; }
    public ushort QType { get; }
    public ushort QClass { get; }
    public byte[] RawRequest { get; }
    public string RequestId { get; }

    public DnsRequestContext(byte[] rawRequest, string requestId, string clientIp)
    {
        RawRequest = rawRequest;
        RequestId = requestId;
        ClientIp = clientIp;
        ClientName = "";

        if (rawRequest.Length >= 12)
        {
            TransactionId = BinaryPrimitives.ReadUInt16BigEndian(rawRequest.AsSpan(0, 2));

            var message = DnsParser.Parse(rawRequest);
            var q = message.Questions.Count > 0 ? message.Questions[0] : null;

            Domain = q?.Name ?? string.Empty;
            QType = q != null ? (ushort)q.Type : (ushort)DnsType.A;
            QClass = q?.Class ?? 1;
        }
        else
        {
            TransactionId = 0;
            Domain = string.Empty;
            QType = (ushort)DnsType.A;
            QClass = 1;
        }
    }
}
