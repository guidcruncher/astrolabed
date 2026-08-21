// File: src/Astrolabed.Dns/Services/IClientNameResolver.cs
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Services;

public interface IClientNameResolver
{
    Task<string> ResolveClientNameAsync(string question, CancellationToken ct = default);
}
