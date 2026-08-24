namespace Astrolabed.Core.Network;

using System.Net;

/// <summary>
/// Extension methods for retrieving IP address information from System.Net.EndPoint instances.
/// </summary>
public static class EndPointExtensions
{
    /// <summary>
    /// Attempts to extract the IPAddress from an EndPoint instance.
    /// </summary>
    /// <param name="endPoint">The network endpoint.</param>
    /// <param name="ipAddress">The extracted IPAddress if the endpoint is an IPEndPoint; otherwise, null.</param>
    /// <returns>True if an IP address was successfully retrieved; otherwise, false.</returns>
    public static bool TryGetIPAddress(this EndPoint? endPoint, out IPAddress? ipAddress)
    {
        if (endPoint is IPEndPoint ipEndPoint)
        {
            ipAddress = ipEndPoint.Address;
            return true;
        }

        ipAddress = null;
        return false;
    }

    /// <summary>
    /// Gets the IPAddress from an EndPoint, throwing an exception if the endpoint is not an IPEndPoint.
    /// </summary>
    /// <param name="endPoint">The network endpoint.</param>
    /// <returns>The associated IPAddress instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when endPoint is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when endPoint cannot be cast to IPEndPoint.</exception>
    public static IPAddress GetIPAddress(this EndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        if (endPoint is IPEndPoint ipEndPoint)
        {
            return ipEndPoint.Address;
        }

        throw new InvalidOperationException($"Cannot extract IP address. Endpoint of type '{endPoint.GetType().FullName}' is not an IPEndPoint.");
    }
}
