using System.Collections;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Provides a read-only list wrapper over <see cref="IHostsManager.Entries"/> to allow dynamic host entry evaluation.
/// </summary>
/// <param name="hostsManager">The underlying hosts manager instance providing entries.</param>
public sealed class HostsEntryListWrapper(IHostsManager hostsManager) : IReadOnlyList<HostsEntry>
{
    private readonly IHostsManager _hostsManager = hostsManager ?? throw new ArgumentNullException(nameof(hostsManager));

    /// <inheritdoc />
    public HostsEntry this[int index]
    {
        get
        {
            IReadOnlyList<HostsEntry>? entries = _hostsManager.Entries;
            if (entries is null)
            {
                throw new IndexOutOfRangeException("Hosts entries collection is currently uninitialized or null.");
            }

            return entries[index];
        }
    }

    /// <inheritdoc />
    public int Count => _hostsManager.Entries?.Count ?? 0;

    /// <inheritdoc />
    public IEnumerator<HostsEntry> GetEnumerator()
    {
        IReadOnlyList<HostsEntry>? entries = _hostsManager.Entries;
        if (entries is null)
        {
            return Enumerable.Empty<HostsEntry>().GetEnumerator();
        }

        return entries.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
