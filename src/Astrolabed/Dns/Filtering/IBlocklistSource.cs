namespace Astrolabed.Dns.Filtering;

public interface IBlocklistSource
{
    Task<IEnumerable<ParsedRule>> LoadAsync();
}
