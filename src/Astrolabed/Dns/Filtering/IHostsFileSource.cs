namespace Astrolabed.Dns.Filtering;

public interface IHostsFileSource
{
    Task<IEnumerable<HostsEntry>> LoadAsync();
}
