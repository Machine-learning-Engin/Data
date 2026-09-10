namespace DataMonitor.Database.Entities;

// Singleton preferences and release maintenance state in the existing database.
public sealed class ApplicationSettingsEntity
{
    public bool StartWithWindows { get; set; }
    public string? NotificationMonth { get; set; }
    public int NotificationLevel { get; set; }
    public DateTime? LastMaintenance { get; set; }
    public int Id { get; set; } = 1;
    public int MonitoringIntervalSeconds { get; set; } = 1;
    public string? SelectedNetworkAdapter { get; set; }
    public long? MonthlyLimitBytes { get; set; }
    public int? WarningPercentage { get; set; }
    public int? CriticalPercentage { get; set; }
}

