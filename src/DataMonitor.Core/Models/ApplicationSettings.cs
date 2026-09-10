namespace DataMonitor.Core.Models;

public sealed class ApplicationSettings
{
    public const int DefaultWarningPercentage = 80;
    public const int DefaultCriticalPercentage = 95;
    public const long BytesPerGB = 1024L * 1024 * 1024;

    public bool StartWithWindows { get; init; }
    public int MonitoringIntervalSeconds { get; init; } = 1;
    // Stable Windows interface ID; null means automatic.
    public string? SelectedNetworkAdapter { get; init; }
    public long? MonthlyLimitBytes { get; init; }
    public int? WarningPercentage { get; init; }
    public int? CriticalPercentage { get; init; }

    public int EffectiveWarningPercentage => WarningPercentage ?? DefaultWarningPercentage;
    public int EffectiveCriticalPercentage => CriticalPercentage ?? DefaultCriticalPercentage;

    public void Validate()
    {
        if (MonitoringIntervalSeconds is < 1 or > 60)
            throw new ArgumentOutOfRangeException(nameof(MonitoringIntervalSeconds),
                "Monitoring interval must be a whole number from 1 to 60 seconds.");
        if (MonthlyLimitBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(MonthlyLimitBytes),
                "Monthly limit cannot be negative.");
        if (EffectiveWarningPercentage is < 0 or > 100 ||
            EffectiveCriticalPercentage is < 0 or > 100)
            throw new ArgumentException("Threshold percentages must be between 0 and 100.");
        if (EffectiveWarningPercentage >= EffectiveCriticalPercentage)
            throw new ArgumentException("Warning percentage must be lower than critical percentage.");
    }
}


