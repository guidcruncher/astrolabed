// File: src/Astrolabed.Dns/Filtering/IReadOnlyDomainFilterRules.cs
namespace Astrolabed.Dns.Filtering;

public interface IReadOnlyDomainFilterRules
{
    IReadOnlySet<string> ExactAllows { get; }
    IReadOnlySet<string> ExactBlocks { get; }
    IReadOnlyList<string> RegexAllows { get; }
    IReadOnlyList<string> RegexBlocks { get; }
}

