// File: src/Astrolabed.Dns/Models/DnsOpCode.cs
namespace Astrolabed.Dns.Models;

public enum DnsOpCode : byte
{
    Query = 0,
    IQuery = 1,
    Status = 2,
    Notify = 4,
    Update = 5
}
