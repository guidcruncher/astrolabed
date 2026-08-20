// File: src/Astrolabed.Dns/Options/DnsEngineOptions.cs
using System;
using System.Collections.Generic;
using System.Net;

namespace Astrolabed.Dns.Options;

public sealed class DnsEngineOptions
{
    public const string SectionName = "DnsEngine";

    public AddressOptions ListenAddress { get; set; } = new();
    public int ProcessingThreads { get; set; } = Math.Max(2, Environment.ProcessorCount);
    public int MaxCacheEntries { get; set; } = 100_000;
    public string LocalDomainSuffix { get; set; } = ".lan";
    public List<string> UpstreamResolvers { get; set; } = new() { "1.1.1.1", "8.8.8.8" };
    public Dictionary<string, string> Hosts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> PtrRecords { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ConditionalPtrRule> ConditionalPtrRules { get; set; } = new();
    public List<string> BlockedDomains { get; set; } = new();
}
