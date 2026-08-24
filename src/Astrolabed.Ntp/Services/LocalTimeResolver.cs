namespace Astrolabed.Ntp.Services;

/// <summary>
/// Provides high-precision time resolution derived directly from the local host system UTC clock.
/// </summary>
public sealed class LocalTimeResolver : ITimeResolver
{
    /// <inheritdoc />
    public ValueTask<DateTimeOffset> GetCurrentTimeAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(DateTimeOffset.UtcNow);
    }
}

