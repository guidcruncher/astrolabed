namespace Astrolabed.Ntp.Services;

public class LocalTimeResolver : ITimeResolver
{
    public ValueTask<DateTimeOffset> GetCurrentTimeAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DateTimeOffset.UtcNow);
    }
}
