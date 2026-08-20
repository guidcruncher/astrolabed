// File: src/Astrolabed.Dns/Filtering/IDomainFilterRuleStore.cs
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Astrolabed.Dns.Filtering;

public interface IDomainFilterRuleStore : IReadOnlyDomainFilterRules
{
    void UpdateRules(IEnumerable<string> allowRules, IEnumerable<string> blockRules);

    (HashSet<string> ExactAllows, List<Regex> RegexAllows, HashSet<string> ExactBlocks, List<Regex> RegexBlocks) GetCompiledSnapshot();
}
