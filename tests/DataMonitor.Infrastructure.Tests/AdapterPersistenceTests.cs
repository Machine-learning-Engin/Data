using DataMonitor.App.ViewModels;
using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.Database;
using DataMonitor.Database.Data;
using DataMonitor.Database.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataMonitor.Infrastructure.Tests;

public sealed class AdapterPersistenceTests
{
    [Fact]
    public async Task SelectionSurvivesReloadUnavailableAdapterAndOtherSettingsSave()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DataMonitorContext>().UseSqlite(connection).Options;
        using (var initial = new DataMonitorContext(options))
        {
            await DatabaseInitializer.InitializeAsync(initial);
            await new ApplicationSettingsRepository(initial).SaveAsync(new ApplicationSettings
            {
                SelectedNetworkAdapter = "offline-id", MonitoringIntervalSeconds = 5,
                MonthlyLimitBytes = 50 * ApplicationSettings.BytesPerGB,
                WarningPercentage = 80, CriticalPercentage = 95
            });
        }
        var adapters = new FakeAdapters();
        using var provider = new ServiceCollection()
            .AddScoped(_ => new DataMonitorContext(options))
            .AddScoped<IApplicationSettingsRepository, ApplicationSettingsRepository>()
            .AddSingleton<INetworkAdapterService>(adapters).BuildServiceProvider();
        var vm = new SettingsViewModel(provider.GetRequiredService<IServiceScopeFactory>());
        await vm.LoadAsync();
        Assert.Equal("offline-id", vm.SelectedAdapterId);
        Assert.Contains(vm.AvailableAdapters, x => x.Id == "offline-id");
        Assert.Contains("Unavailable", vm.SavedAdapterText);
        vm.WarningPercentageText = "75";
        await vm.SaveAsync();
        using var verify = new DataMonitorContext(options);
        var repository = new ApplicationSettingsRepository(verify);
        Assert.Equal("offline-id", (await repository.LoadAsync()).SelectedNetworkAdapter);
        Assert.Equal(75, (await repository.LoadAsync()).WarningPercentage);
        Assert.Equal(50 * ApplicationSettings.BytesPerGB, (await repository.LoadAsync()).MonthlyLimitBytes);
        Assert.Equal(5, (await repository.LoadAsync()).MonitoringIntervalSeconds);

        adapters.Items = [new NetworkAdapterInfo { Id = "online-id", Name = "Wi-Fi" }];
        vm.RefreshAdapters();
        vm.SelectedAdapterId = "online-id";
        await vm.SaveAsync();
        Assert.Equal("online-id", (await repository.LoadAsync()).SelectedNetworkAdapter);
        await vm.LoadAsync();
        Assert.Equal("online-id", vm.SelectedAdapterId);
        vm.SelectedAdapterId = "";
        await vm.SaveAsync();
        Assert.Null((await repository.LoadAsync()).SelectedNetworkAdapter);
        Assert.Single(await verify.ApplicationSettings.ToListAsync());
    }

    [Fact]
    public async Task HistoryAndLimitsIgnoreSourceChangesButRetainLegacyUsage()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var context = new DataMonitorContext(
            new DbContextOptionsBuilder<DataMonitorContext>().UseSqlite(connection).Options);
        await DatabaseInitializer.InitializeAsync(context);
        var repository = new UsageRepository(context);
        var samples = new (string? Source, long Received)[]
        {
            (null, 100), (null, 200), ("a", 10_000), ("a", 10_100),
            ("b", 1_000_000), ("b", 1_000_200), ("a", 10_500), ("a", 10_600)
        };
        for (var i = 0; i < samples.Length; i++)
            await repository.SaveAsync(new NetworkUsage
            {
                Timestamp = new DateTime(2026, 9, 9).AddSeconds(i),
                AdapterName = "Same display name",
                CounterSourceId = samples[i].Source,
                BytesReceived = samples[i].Received
            });
        var statistics = new UsageStatisticsService(context);
        Assert.Equal(500, (await statistics.GetMonthlyUsageAsync(2026, 9)).TotalBytes);
        var points = await statistics.GetChartDataAsync(new DateTime(2026, 9, 9), new DateTime(2026, 9, 10));
        Assert.Equal(4, points.Count);
        Assert.Equal(500 / 1024d / 1024d, points.Sum(x => x.DownloadMB));
        var settings = new ApplicationSettingsRepository(context);
        await settings.SaveAsync(new ApplicationSettings { MonthlyLimitBytes = 1000 });
        Assert.Equal(UsageAlertStatus.Normal,
            (await new UsageAlertService(settings, statistics).GetMonthlyStatusAsync(2026, 9)).Status);
        Assert.Equal(8, await context.UsageRecords.CountAsync());
    }

    private sealed class FakeAdapters : INetworkAdapterService
    {
        public IReadOnlyList<NetworkAdapterInfo> Items { get; set; } = [];
        public IReadOnlyList<NetworkAdapterInfo> GetAvailableAdapters() => Items;
        public IReadOnlyList<NetworkUsage> GetUsageSnapshots() => [];
    }
}
