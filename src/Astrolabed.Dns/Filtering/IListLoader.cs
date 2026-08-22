// File: src/Astrolabed.Dns/Filtering/IListListLoader.cs
namespace Astrolabed.Dns.Filtering;

public interface IListLoader
{
    Task<(List<string> AllowRules, List<string> BlockRules)> LoadRulesAsync(string uriOrPath, CancellationToken ct = default);

    Task LoadAndApplyListAsync(string uriOrPath, CancellationToken ct = default);
}

