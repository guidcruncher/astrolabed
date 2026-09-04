namespace QuickPing.Services;

public interface IPingService
{
    Task<bool> PingAsync(string host, CancellationToken cancellationToken = default);
}
