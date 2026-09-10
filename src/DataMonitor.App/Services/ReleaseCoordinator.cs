using DataMonitor.Database.Services;
using DataMonitor.Infrastructure.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DataMonitor.App.Services;

public sealed class ReleaseCoordinator(IServiceScopeFactory scopeFactory) : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private Task? _loop;
    public void Start() => _loop ??= Task.Run(RunAsync);
    private async Task RunAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<UsageNotificationService>()
                    .CheckAsync(DateTime.Now, _stop.Token);
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { break; }
            catch (Exception ex) { ReleaseLog.Write("Usage notification check failed; retrying in one minute.", ex); }
            try { await Task.Delay(TimeSpan.FromMinutes(1), _stop.Token); }
            catch (OperationCanceledException) { break; }
        }
    }
    public void Dispose() { _stop.Cancel(); }
}
