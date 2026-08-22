namespace Astrolabed.Ntp.Protocol;

public enum NtpLeapIndicator : byte
{
    NoWarning = 0,
    LastMinute61Seconds = 1,
    LastMinute59Seconds = 2,
    Unknown = 3
}

public enum NtpMode : byte
{
    Reserved = 0,
    SymmetricActive = 1,
    SymmetricPassive = 2,
    Client = 3,
    Server = 4,
    Broadcast = 5,
    Control = 6,
    Private = 7
}
