// File: src/Astrolabed.Dns/Filtering/IDomainFilter.cs
namespace Astrolabed.Dns.Filtering;

public interface IDomainFilter
{
    bool IsAllowed(string domain);
    bool IsBlocked(string domain, out string? reason);
}
