namespace Astrolabed.Dhcp.Bootstrap;

public interface IDhcpRuntimeLoader
{
    Task LoadAsync(CancellationToken cancellationToken = default);
}



