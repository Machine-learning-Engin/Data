using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.Database;
using DataMonitor.Database.Data;
using DataMonitor.Database.Entities;
using DataMonitor.Database.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DataMonitor.Infrastructure.Tests;

public sealed class UsageAlertTests
{
    [Theory]
    [InlineData(35, UsageAlertStatus.Normal, 70)]
    [InlineData(40, UsageAlertStatus.Warning, 80)]
    [InlineData(42, UsageAlertStatus.Warning, 84)]
    [InlineData(47.5, UsageAlertStatus.Critical, 95)]
    [InlineData(48, UsageAlertStatus.Critical, 96)]
    [InlineData(50, UsageAlertStatus.Critical, 100)]
    [InlineData(60, UsageAlertStatus.Critical, 120)]
    public void ThresholdsUseExactPercentageAndClampRemaining(double usedGB, UsageAlertStatus status, int percentage)
    {
        var service = new UsageAlertService(null!, null!);
        var result = service.Evaluate((long)(usedGB * ApplicationSettings.BytesPerGB),
            new ApplicationSettings { MonthlyLimitBytes = 50 * ApplicationSettings.BytesPerGB });
        Assert.Equal(status, result.Status);
        Assert.Equal((decimal)percentage, result.UsagePercentage);
        Assert.Equal(Math.Max(0, (long)((50 - usedGB) * ApplicationSettings.BytesPerGB)), result.RemainingBytes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    public void MissingOrZeroLimitIsNotAnAlert(long? limit)
    {
        var result = new UsageAlertService(null!, null!).Evaluate(123,
            new ApplicationSettings { MonthlyLimitBytes = limit });
        Assert.Equal(UsageAlertStatus.NoLimitConfigured, result.Status);
        Assert.Equal(123, result.UsedBytes);
        Assert.Null(result.UsagePercentage);
        Assert.Null(result.RemainingBytes);
    }

    [Fact]
    public void LargeUsageDoesNotOverflowPercentage()
    {
        var result = new UsageAlertService(null!, null!).Evaluate(long.MaxValue,
            new ApplicationSettings { MonthlyLimitBytes = 1 });
        Assert.Equal(long.MaxValue * 100m, result.UsagePercentage);
        Assert.Equal(0, result.RemainingBytes);
        Assert.Equal(UsageAlertStatus.Critical, result.Status);
    }

    [Theory]
    [InlineData(-1L, 80, 95)]
    [InlineData(100L, -1, 95)]
    [InlineData(100L, 80, 101)]
    [InlineData(100L, 95, 95)]
    [InlineData(100L, 96, 95)]
    public async Task InvalidLimitsDoNotPartiallySave(long limit, int warning, int critical)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var context = new DataMonitorContext(
            new DbContextOptionsBuilder<DataMonitorContext>().UseSqlite(connection).Options);
        await DatabaseInitializer.InitializeAsync(context);
        var repository = new ApplicationSettingsRepository(context);
        await Assert.ThrowsAnyAsync<ArgumentException>(() => repository.SaveAsync(
            new ApplicationSettings
            {
                MonitoringIntervalSeconds = 5, MonthlyLimitBytes = limit,
                WarningPercentage = warning, CriticalPercentage = critical
            }));
        var persisted = await repository.LoadAsync();
        Assert.Equal(1, persisted.MonitoringIntervalSeconds);
        Assert.Null(persisted.MonthlyLimitBytes);
    }

    [Fact]
    public async Task MonthlyStatusUsesHistoryStatisticsAndReloadsSavedLimits()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DataMonitorContext>().UseSqlite(connection).Options;
        using var context = new DataMonitorContext(options);
        await DatabaseInitializer.InitializeAsync(context);
        context.UsageRecords.AddRange(
            new UsageRecord { Timestamp = new DateTime(2026, 8, 1), AdapterName = "test", BytesReceived = 1 },
            new UsageRecord { Timestamp = new DateTime(2026, 8, 2), AdapterName = "test", BytesReceived = 100000 },
            new UsageRecord { Timestamp = new DateTime(2026, 9, 1), AdapterName = "test", BytesReceived = 100, BytesSent = 100 },
            new UsageRecord { Timestamp = new DateTime(2026, 9, 2), AdapterName = "test", BytesReceived = 800, BytesSent = 240 });
        await context.SaveChangesAsync();
        var repository = new ApplicationSettingsRepository(context);
        await repository.SaveAsync(new ApplicationSettings
        {
            MonthlyLimitBytes = 1000, WarningPercentage = 80, CriticalPercentage = 95
        });
        var statistics = new UsageStatisticsService(context);
        var service = new UsageAlertService(repository, statistics);
        var september = await service.GetMonthlyStatusAsync(2026, 9);
        Assert.Equal((await statistics.GetMonthlyUsageAsync(2026, 9)).TotalBytes, september.UsedBytes);
        Assert.Equal(840, september.UsedBytes);
        Assert.Equal(UsageAlertStatus.Warning, september.Status);
        Assert.Equal(UsageAlertStatus.NoLimitConfigured,
            (await new UsageAlertService(new DisabledLimits(), statistics).GetMonthlyStatusAsync(2026, 9)).Status);

        using (var writer = new DataMonitorContext(options))
        {
            await new ApplicationSettingsRepository(writer).SaveAsync(new ApplicationSettings
            {
                MonitoringIntervalSeconds = 5,
                MonthlyLimitBytes = 800, WarningPercentage = 70, CriticalPercentage = 90
            });
        }
        Assert.Equal(UsageAlertStatus.Critical, (await service.GetMonthlyStatusAsync(2026, 9)).Status);
        Assert.Equal(UsageAlertStatus.Normal, (await service.GetMonthlyStatusAsync(2026, 10)).Status);
        Assert.Equal(4, await context.UsageRecords.CountAsync());
    }

    private sealed class DisabledLimits : IApplicationSettingsRepository
    {
        public Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ApplicationSettings());
        public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
