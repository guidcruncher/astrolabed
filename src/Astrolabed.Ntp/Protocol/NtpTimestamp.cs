namespace Astrolabed.Ntp.Protocol;

public readonly record struct NtpTimestamp(uint Seconds, uint Fraction)
{
    private static readonly DateTimeOffset NtpEpoch = new(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static NtpTimestamp Zero => new(0, 0);

    public static NtpTimestamp FromDateTimeOffset(DateTimeOffset dateTime)
    {
        DateTimeOffset utcTime = dateTime.ToUniversalTime();
        if (utcTime < NtpEpoch)
        {
            return Zero;
        }

        TimeSpan timeSpan = utcTime - NtpEpoch;
        ulong totalSeconds = (ulong)timeSpan.TotalSeconds;
        uint seconds = (uint)(totalSeconds & 0xFFFFFFFF);

        double fractionPart = timeSpan.TotalSeconds - Math.Truncate(timeSpan.TotalSeconds);
        uint fraction = (uint)(fractionPart * 4294967296.0);

        return new NtpTimestamp(seconds, fraction);
    }

    public DateTimeOffset ToDateTimeOffset()
    {
        if (Seconds == 0 && Fraction == 0)
        {
            return NtpEpoch;
        }

        double fractionSeconds = (double)Fraction / 4294967296.0;
        return NtpEpoch.AddSeconds(Seconds + fractionSeconds);
    }
}
