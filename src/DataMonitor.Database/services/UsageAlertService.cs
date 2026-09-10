using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;

namespace DataMonitor.Database.Services;

public sealed class UsageAlertService(
    IApplicationSettingsRepository settingsRepository,
    IUsageStatisticsService statisticsService) : IUsageAlertService
{
    public async Task<UsageAlertResult> GetMonthlyStatusAsync(
        int year, int month, CancellationToken cancellationToken = default)
    {
        // Sequential calls: both scoped repositories may share one EF context.
        var settings = await settingsRepository.LoadAsync(cancellationToken);
        var usage = await statisticsService.GetMonthlyUsageAsync(year, month, cancellationToken);
        return Evaluate(usage.TotalBytes, settings);
    }

    public UsageAlertResult Evaluate(long usedBytes, ApplicationSettings settings)
    {
        settings.Validate();
        ArgumentOutOfRangeException.ThrowIfNegative(usedBytes);

        if (settings.MonthlyLimitBytes is not > 0)
            return new(UsageAlertStatus.NoLimitConfigured, usedBytes, null, null, null);

        var limit = settings.MonthlyLimitBytes.Value;
        var percentage = usedBytes * 100m / limit;
        var status = percentage >= settings.EffectiveCriticalPercentage
            ? UsageAlertStatus.Critical
            : percentage >= settings.EffectiveWarningPercentage
                ? UsageAlertStatus.Warning
                : UsageAlertStatus.Normal;

        return new(status, usedBytes, limit, Math.Max(0, limit - usedBytes), percentage);
    }
}
