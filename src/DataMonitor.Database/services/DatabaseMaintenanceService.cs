using DataMonitor.Core.Interfaces;
using DataMonitor.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace DataMonitor.Database.Services;

public sealed class DatabaseMaintenanceService(DataMonitorContext context) : IDatabaseMaintenanceService
{
    public const int RetentionDays = 90;

    public async Task<int> RunIfDueAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var last = await context.ApplicationSettings.AsNoTracking()
            .Where(x => x.Id == 1).Select(x => x.LastMaintenance).SingleAsync(cancellationToken);
        if (last.HasValue && now - last.Value < TimeSpan.FromDays(1)) return 0;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // Recheck under the write transaction for concurrent desktop/Worker starts.
        last = await context.ApplicationSettings.AsNoTracking().Where(x => x.Id == 1)
            .Select(x => x.LastMaintenance).SingleAsync(cancellationToken);
        if (last.HasValue && now - last.Value < TimeSpan.FromDays(1)) return 0;
        var cutoff = now.AddDays(-RetentionDays);
        var deleted = await context.UsageRecords.Where(x => x.Timestamp < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
        await context.ApplicationSettings.Where(x => x.Id == 1)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastMaintenance, now), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }
}
