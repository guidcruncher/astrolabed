using System.Collections.Generic;

using Astrolabed.Dns.Core;

namespace Astrolabed.Dns.RuleEngine;

public sealed record RuleResult(List<UpstreamEntry> Upstreams, bool Block);
