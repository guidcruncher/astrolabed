// File: src/Astrolabed.Dns/Services/IClientNameResolver.cs
namespace Astrolabed.Dns.Services;

public interface IClientNameResolver
{
    Task<string> ResolveClientNameAsync(string question, CancellationToken ct = default);
}
