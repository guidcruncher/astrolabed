// File: src/Astrolabed.Dns/Filtering/IListListLoader.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Filtering;

public interface IListLoader
{
    Task<(List<string> AllowRules, List<string> BlockRules)> LoadRulesAsync(string uriOrPath, CancellationToken ct = default);

    Task LoadAndApplyListAsync(string uriOrPath, CancellationToken ct = default);
}

