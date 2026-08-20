// File: src/Astrolabed.Dns/Options/DnsEngineOptions.cs
using System.Collections.Generic;

namespace Astrolabed.Dns.Options;

public enum BlockedResponseMode
{
    Refused,
    NxDomain,
    ServFail,
    ZeroIp,
    CustomIp
}
