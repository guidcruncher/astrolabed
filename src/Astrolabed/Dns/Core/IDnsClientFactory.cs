namespace Astrolabed.Dns.Core
{
    public interface IDnsClientFactory
    {
        IDnsClient Create(Dns.UpstreamResolverOptions resolver);
    }
}
