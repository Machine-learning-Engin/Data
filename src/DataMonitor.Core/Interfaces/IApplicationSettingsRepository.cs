using DataMonitor.Core.Models;

namespace DataMonitor.Core.Interfaces;

public interface IApplicationSettingsRepository
{
    Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}
