
namespace Astrolabed.Dns.Bootstrap;

public interface IDnsForwarderRuntimeLoader
{
    Task LoadAsync(CancellationToken cancellationToken = default);
}

