using DataMonitor.Core.Models;

namespace DataMonitor.Core.Services;

public static class UsageNotificationPolicy
{
    // One notification per severity per calendar month, even after restarting or
    // editing thresholds. Lowering thresholds can trigger an unsent severity.
    public static int PendingLevel(UsageAlertStatus status, int sentLevel) => status switch
    {
        UsageAlertStatus.Critical when sentLevel < 2 => 2,
        UsageAlertStatus.Warning when sentLevel < 1 => 1,
        _ => 0
    };
}
