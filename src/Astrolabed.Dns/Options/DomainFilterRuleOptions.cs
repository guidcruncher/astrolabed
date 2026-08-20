// File: src/Astrolabed.Dns/Options/DomainFilterRuleOptions.cs
using System.Collections.Generic;

namespace Astrolabed.Dns.Options;

public sealed class DomainFilterRuleOptions
{
    public const string SectionName = "DomainFilterRules";

    public List<string> AllowListSources { get; set; } = [];

    public List<string> BlockListSources { get; set; } = [];
}
