using DataMonitor.Core.Models;

namespace DataMonitor.Core.Interfaces;

public interface IUsageAlertService
{
    Task<UsageAlertResult> GetMonthlyStatusAsync(
        int year, int month, CancellationToken cancellationToken = default);

    UsageAlertResult Evaluate(long usedBytes, ApplicationSettings settings);
}
