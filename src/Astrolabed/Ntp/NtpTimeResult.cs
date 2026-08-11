using System;

namespace Astrolabed.Ntp;

public readonly record struct NtpTimeResult(
    DateTime UtcNow,
    TimeSpan Offset,
    int Stratum,
    DateTime ReferenceUtc,
    uint ReferenceId = 0x4C4F434C, // Default ASCII "LOCL"
    byte LeapIndicator = 0);       // 0 = Normal, 3 = Unsynchronized
