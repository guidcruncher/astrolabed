// File: src/Astrolabed.Dns/Resolvers/HostsEntryListWrapper.cs
using System.Collections;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Resolvers;

public sealed class HostsEntryListWrapper : IReadOnlyList<HostsEntry>
{
    private readonly IHostsManager _hostsManager;

    public HostsEntryListWrapper(IHostsManager hostsManager)
    {
        _hostsManager = hostsManager;
    }

    public HostsEntry this[int index] => _hostsManager.Entries[index];

    public int Count => _hostsManager.Entries.Count;

    public IEnumerator<HostsEntry> GetEnumerator() => _hostsManager.Entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
