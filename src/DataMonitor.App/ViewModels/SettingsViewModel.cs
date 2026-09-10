using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.Database;
using DataMonitor.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DataMonitor.App.ViewModels;

public sealed class SettingsViewModel(IServiceScopeFactory scopeFactory) : INotifyPropertyChanged
{
    private string _intervalText = "1";
    private string _monthlyLimitGBText = "";
    private string _warningPercentageText = "80";
    private string _criticalPercentageText = "95";
    private string _statusText = "Loading settings...";
    private bool _canEdit;
    private bool _startWithWindows;
    public string VersionText => "DataMonitor v1.0.0";
    public bool StartWithWindows { get => _startWithWindows; set { _startWithWindows = value; OnPropertyChanged(); } }
    private string _selectedAdapterId = "";
    private string? _savedAdapterId;
    public IReadOnlyList<NetworkAdapterInfo> AvailableAdapters { get; private set; } = [];
    public string AdapterStatusText { get; private set; } = "";
    public string SavedAdapterText { get; private set; } = "Automatic";
    public string SelectedAdapterId
    {
        get => _selectedAdapterId;
        set { _selectedAdapterId = value ?? ""; OnPropertyChanged(); }
    }

    public void RefreshAdapters()
    {
        var selectedId = SelectedAdapterId;
        var choices = new List<NetworkAdapterInfo>
        {
            new() { Id = "", Name = "Automatic — all active adapters" }
        };
        try
        {
            using var scope = scopeFactory.CreateScope();
            choices.AddRange(scope.ServiceProvider.GetRequiredService<INetworkAdapterService>()
                .GetAvailableAdapters());
            AdapterStatusText = "Unavailable selections fall back to automatic until the adapter returns.";
        }
        catch (Exception ex)
        {
            AdapterStatusText = $"Could not refresh adapters: {ex.Message}. Saved selection is retained.";
        }
        foreach (var id in new[] { selectedId, _savedAdapterId })
        {
            if (!string.IsNullOrEmpty(id) && !choices.Any(x => x.Id == id))
                choices.Add(new NetworkAdapterInfo
                {
                    Id = id, Name = "Unavailable adapter", Description = id,
                    OperationalStatus = System.Net.NetworkInformation.OperationalStatus.Down
                });
        }
        AvailableAdapters = choices;
        OnPropertyChanged(nameof(AvailableAdapters));
        SelectedAdapterId = selectedId;
        SavedAdapterText = string.IsNullOrEmpty(_savedAdapterId) ? "Automatic"
            : $"{choices.First(x => x.Id == _savedAdapterId).Name} ({_savedAdapterId})";
        OnPropertyChanged(nameof(SavedAdapterText));
        OnPropertyChanged(nameof(AdapterStatusText));
    }

    public string DatabaseLocation { get; } = DatabasePath.GetDatabasePath();
    public string IntervalText
    {
        get => _intervalText;
        set { _intervalText = value; OnPropertyChanged(); }
    }
    public string MonthlyLimitGBText
    {
        get => _monthlyLimitGBText;
        set { _monthlyLimitGBText = value; OnPropertyChanged(); }
    }
    public string WarningPercentageText
    {
        get => _warningPercentageText;
        set { _warningPercentageText = value; OnPropertyChanged(); }
    }
    public string CriticalPercentageText
    {
        get => _criticalPercentageText;
        set { _criticalPercentageText = value; OnPropertyChanged(); }
    }
    public string StatusText
    {
        get => _statusText;
        private set { _statusText = value; OnPropertyChanged(); }
    }
    public bool CanEdit
    {
        get => _canEdit;
        private set { _canEdit = value; OnPropertyChanged(); }
    }

    public async Task LoadAsync()
    {
        CanEdit = false;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var settings = await scope.ServiceProvider
                .GetRequiredService<IApplicationSettingsRepository>().LoadAsync();
            StartWithWindows = settings.StartWithWindows;
            _savedAdapterId = settings.SelectedNetworkAdapter;
            SelectedAdapterId = _savedAdapterId ?? "";
            RefreshAdapters();
            IntervalText = settings.MonitoringIntervalSeconds.ToString();
            MonthlyLimitGBText = settings.MonthlyLimitBytes.HasValue
                ? (settings.MonthlyLimitBytes.Value / (decimal)ApplicationSettings.BytesPerGB)
                    .ToString(CultureInfo.CurrentCulture)
                : "";
            WarningPercentageText = settings.EffectiveWarningPercentage.ToString();
            CriticalPercentageText = settings.EffectiveCriticalPercentage.ToString();
            StatusText = "Settings loaded. The Worker uses this interval; Dashboard refreshes independently.";
            CanEdit = true;
        }
        catch (Exception ex)
        {
            StatusText = $"Could not load settings. Reopen Settings to retry. {ex.Message}";
        }
    }

    public async Task SaveAsync()
    {
        if (!CanEdit) return;
        if (!int.TryParse(IntervalText, out var interval) || interval is < 1 or > 60)
        {
            StatusText = "Enter a whole number from 1 to 60 seconds. Nothing was saved.";
            return;
        }
        if (!int.TryParse(WarningPercentageText, out var warning) ||
            !int.TryParse(CriticalPercentageText, out var critical))
        {
            StatusText = "Enter whole-number percentages from 0 to 100. Nothing was saved.";
            return;
        }

        long? limitBytes = null;
        if (!string.IsNullOrWhiteSpace(MonthlyLimitGBText))
        {
            if (!decimal.TryParse(MonthlyLimitGBText, NumberStyles.Number,
                    CultureInfo.CurrentCulture, out var gb) ||
                gb < 0 || gb > long.MaxValue / (decimal)ApplicationSettings.BytesPerGB)
            {
                StatusText = "Enter a non-negative monthly limit in GB within the supported byte range. Nothing was saved.";
                return;
            }
            var bytes = decimal.Round(gb * ApplicationSettings.BytesPerGB, 0, MidpointRounding.AwayFromZero);
            if (gb > 0 && bytes < 1)
            {
                StatusText = "A positive monthly limit must be at least one byte. Nothing was saved.";
                return;
            }
            limitBytes = (long)bytes;
        }

        var settings = new ApplicationSettings
        {
            StartWithWindows = StartWithWindows,
            MonitoringIntervalSeconds = interval,
            SelectedNetworkAdapter = string.IsNullOrWhiteSpace(SelectedAdapterId) ? null : SelectedAdapterId,
            MonthlyLimitBytes = limitBytes,
            WarningPercentage = warning,
            CriticalPercentage = critical
        };
        try
        {
            settings.Validate();
        }
        catch (ArgumentException ex)
        {
            StatusText = $"{ex.Message} Nothing was saved.";
            return;
        }

        CanEdit = false;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IApplicationSettingsRepository>();
            var startup = scope.ServiceProvider.GetService<IStartupRegistration>();
            if (startup is null) await repository.SaveAsync(settings);
            else await new StartupSettingsService(repository, startup).SaveAsync(settings);
            StartWithWindows = settings.StartWithWindows;
            _savedAdapterId = settings.SelectedNetworkAdapter;
            RefreshAdapters();
            StatusText = "Settings saved. Dashboard applies the adapter on its next sample; Worker applies it after its current wait.";
        }
        catch (Exception ex)
        {
            StatusText = $"Could not save settings. Try again. {ex.Message}";
        }
        finally
        {
            CanEdit = true;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}


