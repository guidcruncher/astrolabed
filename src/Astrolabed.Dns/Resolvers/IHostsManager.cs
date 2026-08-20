// File: src/Astrolabed.Dns/Resolvers/IHostsManager.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Resolvers;

public interface IHostsManager
{
    IReadOnlyList<HostsEntry> Entries { get; }
    Task ReloadAsync(CancellationToken ct = default);
}
