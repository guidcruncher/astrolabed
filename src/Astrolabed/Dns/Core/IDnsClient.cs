namespace Astrolabed.Dns.Core;

public interface IDnsClient
{
    Task<byte[]> QueryAsync(byte[] request, CancellationToken ct);
}
