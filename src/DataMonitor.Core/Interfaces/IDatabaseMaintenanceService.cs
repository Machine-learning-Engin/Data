namespace DataMonitor.Core.Interfaces;

public interface IDatabaseMaintenanceService
{
    Task<int> RunIfDueAsync(DateTime now, CancellationToken cancellationToken = default);
}
