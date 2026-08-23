// File: src/Astrolabed.Dns/Models/EngineStateSnapshot.cs
using System.Collections.Immutable;
using System.Net;

namespace Astrolabed.Dns.Models;

/// <summary>
/// Represents an immutable snapshot of the DNS engine's current runtime state and lookup tables.
/// </summary>
/// <param name="Hosts">The current immutable mapping of hostname strings to host IP addresses.</param>
/// <param name="PtrRecords">The current immutable mapping of reverse DNS lookup strings to names.</param>
/// <param name="BlockedDomains">The current immutable collection of blocked domain names.</param>
/// <param name="UpstreamResolvers">The current immutable list of active upstream resolver IP endpoints.</param>
public sealed record EngineStateSnapshot(
    ImmutableDictionary<string, IPAddress> Hosts,
    ImmutableDictionary<string, string> PtrRecords,
    ImmutableHashSet<string> BlockedDomains,
    ImmutableList<IPAddress> UpstreamResolvers
);
