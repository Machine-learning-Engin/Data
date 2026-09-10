using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;

namespace DataMonitor.Core.Services;

public sealed class StartupSettingsService(IApplicationSettingsRepository repository, IStartupRegistration registration)
{
    public async Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        settings.Validate();
        var wasEnabled = registration.IsEnabled;
        registration.SetEnabled(settings.StartWithWindows);
        try { await repository.SaveAsync(settings, cancellationToken); }
        catch
        {
            // The database remains authoritative if its atomic save fails.
            registration.SetEnabled(wasEnabled);
            throw;
        }
    }
}
