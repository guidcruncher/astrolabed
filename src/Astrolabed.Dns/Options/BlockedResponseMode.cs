// File: src/Astrolabed.Dns/Options/DnsEngineOptions.cs
namespace Astrolabed.Dns.Options;

public enum BlockedResponseMode
{
    Refused,
    NxDomain,
    ServFail,
    ZeroIp,
    CustomIp
}
