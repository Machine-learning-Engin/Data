using DataMonitor.App.ViewModels;
using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DataMonitor.Infrastructure.Tests;

public sealed class UsageLimitViewModelTests
{
    [Theory]
    [InlineData("-1", "80", "95")]
    [InlineData("50", "95", "95")]
    [InlineData("50", "96", "95")]
    [InlineData("50", "-1", "95")]
    [InlineData("50", "80", "101")]
    [InlineData("not a number", "80", "95")]
    [InlineData("999999999999999999999999999999999", "80", "95")]
    [InlineData("50", "80.5", "95")]
    public async Task SettingsRejectInvalidInputWithoutSaving(string limit, string warning, string critical)
    {
        var repository = new MemorySettings();
        using var provider = new ServiceCollection()
            .AddSingleton<IApplicationSettingsRepository>(repository).BuildServiceProvider();
        var vm = new SettingsViewModel(provider.GetRequiredService<IServiceScopeFactory>());
        await vm.LoadAsync();
        vm.MonthlyLimitGBText = limit;
        vm.WarningPercentageText = warning;
        vm.CriticalPercentageText = critical;
        await vm.SaveAsync();
        Assert.Equal(0, repository.Saves);
        Assert.Contains("Nothing was saved", vm.StatusText);
        Assert.True(vm.CanEdit);
    }

    [Fact]
    public async Task SettingsRoundTripAndClearLimit()
    {
        var repository = new MemorySettings();
        using var provider = new ServiceCollection()
            .AddSingleton<IApplicationSettingsRepository>(repository).BuildServiceProvider();
        var vm = new SettingsViewModel(provider.GetRequiredService<IServiceScopeFactory>());
        await vm.LoadAsync();
        Assert.Equal("80", vm.WarningPercentageText);
        Assert.Equal("95", vm.CriticalPercentageText);
        vm.MonthlyLimitGBText = "50";
        vm.WarningPercentageText = "75";
        vm.CriticalPercentageText = "90";
        vm.IntervalText = "5";
        await vm.SaveAsync();
        Assert.Equal(50 * ApplicationSettings.BytesPerGB, repository.Settings.MonthlyLimitBytes);
        Assert.Equal(5, repository.Settings.MonitoringIntervalSeconds);
        await vm.LoadAsync();
        Assert.Equal("50", vm.MonthlyLimitGBText);
        Assert.Equal("75", vm.WarningPercentageText);
        Assert.Equal("90", vm.CriticalPercentageText);
        vm.MonthlyLimitGBText = "";
        await vm.SaveAsync();
        Assert.Null(repository.Settings.MonthlyLimitBytes);
        vm.MonthlyLimitGBText = "0";
        await vm.SaveAsync();
        Assert.Equal(0, repository.Settings.MonthlyLimitBytes);
    }

    [Fact]
    public async Task MonthlyCardShowsOverageAndClearsStaleDataOnFailure()
    {
        var service = new FakeAlerts();
        using var provider = new ServiceCollection()
            .AddSingleton<IUsageAlertService>(service).BuildServiceProvider();
        var vm = new MonthlyUsageViewModel(provider.GetRequiredService<IServiceScopeFactory>());
        await vm.RefreshAsync();
        Assert.Equal(DateTime.Now.Month, service.Month);
        Assert.Equal(DateTime.Now.Year, service.Year);
        Assert.Equal(100, vm.Progress);
        Assert.Equal("120%", vm.PercentageText);
        Assert.Contains("Critical", vm.StatusText);
        Assert.Equal("0 GB remaining", vm.RemainingText);
        service.Fail = true;
        await vm.RefreshAsync();
        Assert.Equal("—", vm.UsedText);
        Assert.Equal(0, vm.Progress);
        Assert.Contains("unavailable", vm.StatusText);
        service.Fail = false;
        await vm.RefreshAsync();
        Assert.Contains("Critical", vm.StatusText);
    }

    private sealed class MemorySettings : IApplicationSettingsRepository
    {
        public ApplicationSettings Settings { get; private set; } = new();
        public int Saves { get; private set; }
        public Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Settings);
        public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
        {
            settings.Validate();
            Settings = settings;
            Saves++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAlerts : IUsageAlertService
    {
        public bool Fail { get; set; }
        public int Year { get; private set; }
        public int Month { get; private set; }
        public Task<UsageAlertResult> GetMonthlyStatusAsync(int year, int month, CancellationToken cancellationToken = default)
        {
            Year = year;
            Month = month;
            if (Fail) throw new InvalidOperationException("Simulated database error");
            return Task.FromResult(new UsageAlertResult(UsageAlertStatus.Critical,
                60 * ApplicationSettings.BytesPerGB, 50 * ApplicationSettings.BytesPerGB, 0, 120));
        }
        public UsageAlertResult Evaluate(long usedBytes, ApplicationSettings settings)
            => throw new NotSupportedException();
    }
}
