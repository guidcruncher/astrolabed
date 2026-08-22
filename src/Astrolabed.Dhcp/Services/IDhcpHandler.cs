using Astrolabed.Dhcp.Protocol;

namespace Astrolabed.Dhcp.Services;

public interface IDhcpHandler
{
    Task<DhcpMessage?> ProcessMessageAsync(DhcpMessage request, CancellationToken cancellationToken = default);
}
