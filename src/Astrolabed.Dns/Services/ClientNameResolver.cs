// File: src/Astrolabed.Dns/Services/ClientNameResolver.cs
using System.Net;

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
    public async Task<string?> ResolveClientNameAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDiscoveredLanDeviceRepository>();

        LogResolvingClientName(_logger, ipAddress);

        var device = await repository.GetByIpAddressAsync(IPAddress.Parse(ipAddress), cancellationToken).ConfigureAwait(false);
        return device?.HostName;
    }

    [LoggerMessage(
        EventId = 901,
        Level = LogLevel.Debug,
        Message = "Resolving LAN client name for IP address: {IpAddress}")]
    private static partial void LogResolvingClientName(ILogger logger, string ipAddress);
}
