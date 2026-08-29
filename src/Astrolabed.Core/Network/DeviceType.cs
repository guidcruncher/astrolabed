namespace Astrolabed.Core.Network;

/// <summary>
/// Specifies categories of network devices recognized by the network scanner heuristic engine.
/// </summary>
public enum DeviceType
{
    /// <summary>
    /// An unidentified or unrecognized network device.
    /// </summary>
    Unknown,

    /// <summary>
    /// An Apple iPhone smartphone running iOS.
    /// </summary>
    iPhone,

    /// <summary>
    /// An Apple iPad tablet running iPadOS.
    /// </summary>
    iPad,

    /// <summary>
    /// A generic Apple hardware device (e.g., Mac, Apple TV) where granular subtype detection was inconclusive.
    /// </summary>
    Apple,

    /// <summary>
    /// A Personal Computer, typically running Microsoft Windows.
    /// </summary>
    PC,

    /// <summary>
    /// A smartphone or tablet device running the Android operating system.
    /// </summary>
    Android,

    /// <summary>
    /// A network routing device, default gateway, wireless access point, or managed switch.
    /// </summary>
    Router,

    /// <summary>
    /// A computer, server, or appliance running a Linux operating system distribution.
    /// </summary>
    Linux,

    /// <summary>
    /// An Internet of Things (IoT) peripheral, embedded controller, or smart home device.
    /// </summary>
    IoT,

    /// <summary>
    /// A Nintendo gaming console (e.g., Nintendo Switch).
    /// </summary>
    Nintendo,

    /// <summary>
    /// A Sony PlayStation gaming console.
    /// </summary>
    Playstation,

    /// <summary>
    /// A Microsoft Xbox gaming console.
    /// </summary>
    XBOX,

    /// <summary>
    /// A Smart TV, streaming media receiver, or digital media renderer.
    /// </summary>
    SmartTV
}
