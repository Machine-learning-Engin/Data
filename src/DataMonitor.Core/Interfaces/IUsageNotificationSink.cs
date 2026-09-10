using DataMonitor.Core.Models;

namespace DataMonitor.Core.Interfaces;

public interface IUsageNotificationSink
{
    Task ShowAsync(UsageAlertStatus status, decimal percentage, CancellationToken cancellationToken = default);
}
