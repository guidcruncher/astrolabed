namespace Astrolabed.Core.Network;
/// <summary>
/// Defines the contract for classifying network devices into functional categories.
/// </summary>
public interface INetworkDeviceClassifier
{
    /// <summary>
    /// Analyzes network probe results and MAC vendor information to determine device type.
    /// </summary>
    /// <param name="probeResult">The collected network artifacts for the target device.</param>
    /// <returns>The resolved <see cref="DeviceType"/>.</returns>
    DeviceType ClassifyDevice(NetworkDeviceProbeResult probeResult);
}

