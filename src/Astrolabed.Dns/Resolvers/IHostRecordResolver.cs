// File: src/Astrolabed.Dns/Resolvers/IHostRecordResolver.cs
using System.Net;
using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Resolvers;

public interface IHostRecordResolver
{
    bool TryResolveHost(string domain, DnsType recordType, out IPAddress? address);
}

