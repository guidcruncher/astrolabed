using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp.Bootstrap;

public sealed class DhcpRuntimeLoader : IDhcpRuntimeLoader
{
    private readonly DhcpOptions _options;
    private readonly IDhcpLeaseStore _leaseStore;

    public DhcpRuntimeLoader(
        IOptions<ServerOptions> options,
        IDhcpLeaseStore leaseStore)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(leaseStore);

        _options = options.Value.Dhcp;
        _leaseStore = leaseStore;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (_leaseStore is JsonDhcpLeaseStore jsonStore && !string.IsNullOrWhiteSpace(_options.LeaseStorePath) && File.Exists(_options.LeaseStorePath))
        {
            await jsonStore.LoadAsync().ConfigureAwait(false);
        }
    }
}
