using System.ComponentModel;

using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DataMonitor.App.ViewModels;

public sealed class MonthlyUsageViewModel(IServiceScopeFactory scopeFactory) : INotifyPropertyChanged
{
    private bool _loading;
    private UsageAlertResult? _result;
    private string _error = "";
    private string _periodText = "Recorded monthly usage";

    public string PeriodText => _periodText;
    public string UsedText => _result is null ? "—" : $"{ToGB(_result.UsedBytes):0.##} GB used";
    public string LimitText => _result is null ? "—"
        : _result.LimitBytes is long limit ? $"{ToGB(limit):0.##} GB limit" : "No limit configured";
    public string RemainingText => _result?.RemainingBytes is long remaining
        ? $"{ToGB(remaining):0.##} GB remaining" : "Remaining: —";
    public string PercentageText => _result?.UsagePercentage is decimal percentage
        ? $"{percentage:0.##}%" : "Progress: —";
    public double Progress => (double)Math.Clamp(_result?.UsagePercentage ?? 0, 0m, 100m);
    public string StatusText => _error.Length > 0 ? _error : _result?.Status switch
    {
        UsageAlertStatus.Normal => "Normal",
        UsageAlertStatus.Warning => "Warning — approaching monthly limit",
        UsageAlertStatus.Critical => "Critical — at or above critical threshold",
        UsageAlertStatus.NoLimitConfigured => "No limit configured",
        _ => "Loading monthly usage..."
    };
    public string StatusColor => _error.Length > 0 ? "#FBBF24" : _result?.Status switch
    {
        UsageAlertStatus.Warning => "#FBBF24",
        UsageAlertStatus.Critical => "#F87171",
        UsageAlertStatus.Normal => "#4ADE80",
        _ => "#A7AFBD"
    };

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_loading) return;
        _loading = true;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var now = DateTime.Now;
            var result = await scope.ServiceProvider.GetRequiredService<IUsageAlertService>()
                .GetMonthlyStatusAsync(now.Year, now.Month, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _result = result;
            _error = "";
            _periodText = $"{now:MMMM yyyy} · Recorded usage";
            OnPropertyChanged();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Navigation away cancels the query without publishing stale results.
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _result = null;
                _error = $"Monthly usage unavailable: {ex.Message}";
                OnPropertyChanged();
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private static decimal ToGB(long bytes) => bytes / (decimal)ApplicationSettings.BytesPerGB;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}


