namespace Astrolabed.Data.Services;

public interface ISchemaProvider
{
    Task<string> GetSchemaSqlAsync(CancellationToken cancellationToken = default);
}
