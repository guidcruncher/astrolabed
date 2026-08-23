namespace Astrolabed.Ntp.Services;

/// <summary>
/// Defines a contract for high-precision, asynchronous time resolution used by NTP and DHCP engine services.
/// </summary>
public interface ITimeResolver
{
    /// <summary>
    /// Asynchronously retrieves the current high-precision <see cref="DateTimeOffset"/> in UTC.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the current UTC timestamp.</returns>
    ValueTask<DateTimeOffset> GetCurrentTimeAsync(CancellationToken cancellationToken = default);
}
