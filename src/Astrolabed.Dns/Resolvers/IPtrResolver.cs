// File: src/Astrolabed.Dns/Resolvers/IPtrResolver.cs
namespace Astrolabed.Dns.Resolvers;

public interface IPtrResolver
{
    bool TryResolvePtr(string ptrQuery, out string? domainName);
}
