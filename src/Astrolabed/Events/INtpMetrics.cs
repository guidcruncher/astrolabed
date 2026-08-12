namespace Astrolabed.Events;

public interface INtpMetrics
{
    void Sync(NtpSyncEvent evt);
}
