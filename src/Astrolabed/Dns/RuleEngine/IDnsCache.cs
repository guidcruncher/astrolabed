using System;

using Astrolabed.Dns.Core;

namespace Astrolabed.Dns.RuleEngine;

public interface IDnsCache : IDisposable
{
    Guid InstanceId { get; }

    bool TryGet(in DnsRequestContext context, out byte[]? response);

    void Store(in DnsRequestContext context, byte[] response, TimeSpan ttl);

    void Flush();
}
