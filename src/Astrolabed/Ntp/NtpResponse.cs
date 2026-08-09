namespace Astrolabed.Ntp;

public sealed record NtpResponse(
    bool Success,
    TimeSpan Offset,
    byte[]? Bytes);
