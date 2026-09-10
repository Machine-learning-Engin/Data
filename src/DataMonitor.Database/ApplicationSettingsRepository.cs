using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace DataMonitor.Database;

public sealed class ApplicationSettingsRepository(DataMonitorContext context)
    : IApplicationSettingsRepository
{
    public async Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await context.ApplicationSettings.AsNoTracking()
            .Where(x => x.Id == 1)
            .Select(x => new ApplicationSettings
            {
                StartWithWindows = x.StartWithWindows,
                MonitoringIntervalSeconds = x.MonitoringIntervalSeconds,
                SelectedNetworkAdapter = x.SelectedNetworkAdapter,
                MonthlyLimitBytes = x.MonthlyLimitBytes,
                WarningPercentage = x.WarningPercentage,
                CriticalPercentage = x.CriticalPercentage
            })
            .SingleAsync(cancellationToken);

        settings.Validate();
        return settings;
    }

    public async Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        settings.Validate();
        // Save all settings atomically in the existing singleton row.
        var updated = await context.ApplicationSettings.Where(x => x.Id == 1)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.StartWithWindows, settings.StartWithWindows)
                .SetProperty(x => x.MonitoringIntervalSeconds, settings.MonitoringIntervalSeconds)
                .SetProperty(x => x.SelectedNetworkAdapter,
                    string.IsNullOrWhiteSpace(settings.SelectedNetworkAdapter) ? null : settings.SelectedNetworkAdapter)
                .SetProperty(x => x.MonthlyLimitBytes, settings.MonthlyLimitBytes)
                .SetProperty(x => x.WarningPercentage, settings.WarningPercentage)
                .SetProperty(x => x.CriticalPercentage, settings.CriticalPercentage),
                cancellationToken);
        if (updated != 1)
            throw new InvalidOperationException("Application settings are missing. Restart DataMonitor to initialize them.");
    }
}


