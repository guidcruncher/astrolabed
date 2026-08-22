// File: src/Astrolabed.Dns/Models/EngineStateSnapshot.cs
using System.Collections.Immutable;
using System.Net;

namespace Astrolabed.Dns.Models;

public sealed record EngineStateSnapshot(
    ImmutableDictionary<string, IPAddress> Hosts,
    ImmutableDictionary<string, string> PtrRecords,
    ImmutableHashSet<string> BlockedDomains,
    ImmutableList<IPAddress> UpstreamResolvers
);
