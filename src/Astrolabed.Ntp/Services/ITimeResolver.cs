namespace Astrolabed.Ntp.Services;

public interface ITimeResolver
{
    ValueTask<DateTimeOffset> GetCurrentTimeAsync(CancellationToken cancellationToken = default);
}
