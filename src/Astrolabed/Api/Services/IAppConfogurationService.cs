using Astrolabed;

namespace Astrolabed.Api.Services;

public interface IAppConfigurationService
{
    ServerOptions GetConfiguration();
    Task UpdateConfigurationAsync(ServerOptions newConfig, CancellationToken cancellationToken = default);
}
