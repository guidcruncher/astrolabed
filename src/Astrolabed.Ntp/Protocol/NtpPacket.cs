namespace Astrolabed.Ntp.Protocol;

public class NtpPacket
{
    public NtpLeapIndicator LeapIndicator { get; set; } = NtpLeapIndicator.NoWarning;
    public byte VersionNumber { get; set; } = 4;
    public NtpMode Mode { get; set; } = NtpMode.Server;
    public byte Stratum { get; set; } = 1;
    public sbyte Poll { get; set; } = 4;
    public sbyte Precision { get; set; } = -20;
    public uint RootDelay { get; set; }
    public uint RootDispersion { get; set; }
    public uint ReferenceIdentifier { get; set; }
    public NtpTimestamp ReferenceTimestamp { get; set; } = NtpTimestamp.Zero;
    public NtpTimestamp OriginTimestamp { get; set; } = NtpTimestamp.Zero;
    public NtpTimestamp ReceiveTimestamp { get; set; } = NtpTimestamp.Zero;
    public NtpTimestamp TransmitTimestamp { get; set; } = NtpTimestamp.Zero;
    public byte[] ExtensionFields { get; set; } = Array.Empty<byte>();
}
