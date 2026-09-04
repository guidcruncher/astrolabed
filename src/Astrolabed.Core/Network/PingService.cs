using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickPing.Configuration;

namespace QuickPing.Services;

public class PingService : IPingService
{
    private readonly ILogger<PingService> _logger;
    private readonly PingServiceOptions _options;

    public PingService(
        ILogger<PingService> logger,
        IOptions<PingServiceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<bool> PingAsync(string host, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host cannot be null or empty.", nameof(host));
        }

        using var ping = new Ping();
        var pingOptions = new PingOptions(_options.Ttl, _options.DontFragment);
        byte[] buffer = new byte[32];

        try
        {
            _logger.LogInformation("Sending ping request to {Host} with timeout {Timeout}ms.", host, _options.TimeoutMilliseconds);

            PingReply reply = await ping.SendPingAsync(
                host,
                _options.TimeoutMilliseconds,
                buffer,
                pingOptions);

            if (reply.Status == IPStatus.Success)
            {
                _logger.LogInformation("Ping to {Host} succeeded. Time: {RoundtripTime}ms, IP: {Address}", host, reply.RoundtripTime, reply.Address);
                return true;
            }

            _logger.LogWarning("Ping to {Host} failed. Status: {Status}", host, reply.Status);
            return false;
        }
        catch (PingException ex)
        {
            _logger.LogError(ex, "An error occurred while pinging host {Host}.", host);
            return false;
        }
    }
}
