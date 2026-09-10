namespace DataMonitor.Core.Models;

public sealed record UsageAlertResult(
    UsageAlertStatus Status,
    long UsedBytes,
    long? LimitBytes,
    long? RemainingBytes,
    decimal? UsagePercentage);
