using System.Text.RegularExpressions;

using Astrolabed.Dns.Core;

namespace Astrolabed.Dns.RuleEngine;

public sealed record CompiledRule(
    string Pattern,
    IDnsClient? Client,
    bool Block,
    string Name,
    Regex? Regex);

