// File: src/Astrolabed.Dns/Services/ClientNameResolver.cs
using Astrolabed.Data.Repositories;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Resolves LAN client display names using dynamic scopes to safely consume scoped repositories from a singleton context.
/// </summary>
public sealed partial class ClientNameResolver : IClientNameResolver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClientNameResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientNameResolver"/> class.
    /// </summary>
    /// <param name="scopeFactory">Service scope factory for resolving scoped repositories on demand.</param>
    /// <param name="logger">Structured logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required argument is <c>null</c>.</exception>
    public ClientNameResolver(
        IServiceScopeFactory scopeFactory,
        ILogger<ClientNameResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> ResolveClientNameAsync(string ptrAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ptrAddress))
        {
            return string.Empty;
        }

        // 1. Check ARP table
        using IServiceScope scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDiscoveredLanDeviceRepository>();
        var dhcpRepository = scope.ServiceProvider.GetRequiredService<IDhcpLeaseRepository>();
        LogResolvingClientName(_logger, ptrAddress);

        var device = await repository.GetByPtrAddressAsync(ptrAddress, cancellationToken).ConfigureAwait(false);
        if (device != null && !string.IsNullOrWhiteSpace(device.HostName))
        {
            return device.HostName;
        }

        // 2. Check Local DHCP Leases (if enabled)
        var dhcpDevice = await dhcpRepository.GetLeaseByPtrAddressAsync(ptrAddress, cancellationToken).ConfigureAwait(false);
        if (dhcpDevice != null && !string.IsNullOrWhiteSpace(dhcpDevice.ClientName))
        {
            return dhcpDevice.ClientName;
        }

        // 3. Check Conditional Forwarding

        return string.Empty;
    }

    [LoggerMessage(
        EventId = 901,
        Level = LogLevel.Debug,
        Message = "Resolving LAN client name for PTR address: {PtrAddress}")]
    private static partial void LogResolvingClientName(ILogger logger, string ptrAddress);
}
