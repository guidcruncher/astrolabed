namespace Astrolabed.Ntp.Protocol;

/// <summary>
/// Represents the RFC 5905 2-bit Leap Indicator (LI) warning of impending leap second insertion or deletion.
/// </summary>
public enum NtpLeapIndicator : byte
{
    /// <summary>
    /// Indicates no leap second warning or adjustment is pending.
    /// </summary>
    NoWarning = 0,

    /// <summary>
    /// Warns that the last minute of the current day has 61 seconds (leap second insertion).
    /// </summary>
    LastMinute61Seconds = 1,

    /// <summary>
    /// Warns that the last minute of the current day has 59 seconds (leap second deletion).
    /// </summary>
    LastMinute59Seconds = 2,

    /// <summary>
    /// Indicates the server clock state is unsynchronized or unknown.
    /// </summary>
    Unknown = 3
}

/// <summary>
/// Represents the RFC 5905 3-bit NTP packet operation mode.
/// </summary>
public enum NtpMode : byte
{
    /// <summary>
    /// Reserved value for operational or internal use.
    /// </summary>
    Reserved = 0,

    /// <summary>
    /// Indicates a symmetric active association mode.
    /// </summary>
    SymmetricActive = 1,

    /// <summary>
    /// Indicates a symmetric passive association mode.
    /// </summary>
    SymmetricPassive = 2,

    /// <summary>
    /// Indicates a standard client request mode.
    /// </summary>
    Client = 3,

    /// <summary>
    /// Indicates a standard server response mode.
    /// </summary>
    Server = 4,

    /// <summary>
    /// Indicates an NTP broadcast or multicast server mode.
    /// </summary>
    Broadcast = 5,

    /// <summary>
    /// Reserved for NTP control messages (Mode 6).
    /// </summary>
    Control = 6,

    /// <summary>
    /// Reserved for private experimentation or vendor implementations (Mode 7).
    /// </summary>
    Private = 7
}
