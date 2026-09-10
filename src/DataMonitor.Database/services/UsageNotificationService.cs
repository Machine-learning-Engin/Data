using System.Globalization;
using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.Core.Services;
using DataMonitor.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace DataMonitor.Database.Services;

public sealed class UsageNotificationService(DataMonitorContext context, IUsageAlertService alerts,
    IUsageNotificationSink sink)
{
    public async Task CheckAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var result = await alerts.GetMonthlyStatusAsync(now.Year, now.Month, cancellationToken);
        var month = now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var state = await context.ApplicationSettings.AsNoTracking().SingleAsync(x => x.Id == 1, cancellationToken);
        var sent = state.NotificationMonth == month ? state.NotificationLevel : 0;
        var level = UsageNotificationPolicy.PendingLevel(result.Status, sent);
        // Persist the claim before calling Windows. This deliberately favors no
        // duplicate notifications after a crash over guaranteed delivery.
        await context.ApplicationSettings.Where(x => x.Id == 1).ExecuteUpdateAsync(s => s
            .SetProperty(x => x.NotificationMonth, month)
            .SetProperty(x => x.NotificationLevel, level == 0 ? sent : level), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (level > 0)
            await sink.ShowAsync(result.Status, result.UsagePercentage!.Value, cancellationToken);
    }
}
