// File: src/Astrolabed.Dns/Options/DnsEngineOptions.cs
namespace Astrolabed.Dns.Options;

using System;
using System.Collections.Generic;

using Astrolabed.Core.Options;

/// <summary>
/// Configures settings for the DNS server engine, including listening bindings, threading parameters, cache capacity, upstream resolvers, and filtering behaviors.
/// </summary>
public sealed class DnsEngineOptions
{
    /// <summary>
    /// The configuration section key path name used when binding settings from application configuration sources.
    /// </summary>
    public const string SectionName = "DnsEngine";

    /// <summary>
    /// Gets or sets the network binding and address options configured for listening for incoming DNS queries.
    /// </summary>
    public AddressOptions ListenAddress { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum number of worker threads allocated for processing DNS requests.
    /// </summary>
    public int ProcessingThreads { get; set; } = Math.Max(2, Environment.ProcessorCount);

    /// <summary>
    /// Gets or sets the maximum number of record entries permitted in the internal DNS cache.
    /// </summary>
    public int MaxCacheEntries { get; set; } = 100_000;

    /// <summary>
    /// Gets or sets the default local domain suffix used for resolving local network hostname queries.
    /// </summary>
    public string LocalDomainSuffix { get; set; } = ".lan";

    /// <summary>
    /// Gets or sets the list of upstream DNS resolver IP addresses used for forwarding external queries.
    /// </summary>
    public List<string> UpstreamResolvers { get; set; } = [];

    /// <summary>
    /// Gets or sets static hosts mapping entries (similar to a standard hosts file).
    /// </summary>
    public List<string> Hosts { get; set; } = [];

    /// <summary>
    /// Gets or sets explicit PTR reverse DNS lookup mapping entries.
    /// </summary>
    public Dictionary<string, string> PtrRecords { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets conditional forwarding rules for targeted PTR record lookups.
    /// </summary>
    public List<PtrConditionalRule> ConditionalPtrRules { get; set; } = [];

    /// <summary>
    /// Gets or sets the response mode executed when an incoming query matches a blocking filter rule.
    /// </summary>
    public BlockedResponseMode BlockedResponseMode { get; set; } = BlockedResponseMode.Refused;

    /// <summary>
    /// Gets or sets the custom IP address returned when <see cref="BlockedResponseMode"/> is set to <see cref="BlockedResponseMode.CustomIp"/>.
    /// </summary>
    public string CustomBlockedIp { get; set; } = "0.0.0.0";
}
