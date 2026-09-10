using DataMonitor.Core.Models;

namespace DataMonitor.Core.Interfaces;

public interface IUsageRepository
{
    Task SaveAsync(
        NetworkUsage usage,
        CancellationToken cancellationToken = default);
}