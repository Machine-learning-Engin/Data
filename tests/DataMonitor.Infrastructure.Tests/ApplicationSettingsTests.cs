using DataMonitor.Core.Models;
using DataMonitor.Database;
using DataMonitor.Database.Data;
using DataMonitor.Database.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DataMonitor.Infrastructure.Tests;

public sealed class ApplicationSettingsTests
{
    [Theory]
    [InlineData("fresh")]
    [InlineData("usage-only")]
    [InlineData("interval-only")]
    public async Task UpgradePreservesSettingsUsageAndHistory(string schema)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DataMonitorContext>().UseSqlite(connection).Options;

        using (var context = new DataMonitorContext(options))
        {
            if (schema != "fresh")
            {
                // Exact legacy table shape; do not use the new EF model to simulate an upgrade.
                await context.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE UsageRecords (
                        Id INTEGER NOT NULL CONSTRAINT PK_UsageRecords PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL, AdapterName TEXT NOT NULL,
                        BytesReceived INTEGER NOT NULL, BytesSent INTEGER NOT NULL);
                    CREATE INDEX IX_UsageRecords_Timestamp ON UsageRecords (Timestamp);
                    CREATE INDEX IX_UsageRecords_AdapterName ON UsageRecords (AdapterName);
                    INSERT INTO UsageRecords VALUES
                        (1, '2026-09-09 12:00:00', 'Existing adapter', 1000, 2000),
                        (2, '2026-09-09 12:00:05', 'Existing adapter', 5000, 4000);
                    """);
            }
            if (schema == "interval-only")
            {
                await context.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE ApplicationSettings (
                        Id INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
                        MonitoringIntervalSeconds INTEGER NOT NULL DEFAULT 1
                            CHECK (MonitoringIntervalSeconds BETWEEN 1 AND 60));
                    INSERT INTO ApplicationSettings VALUES (1, 5);
                    """);
            }

            await DatabaseInitializer.InitializeAsync(context);
            var repository = new ApplicationSettingsRepository(context);
            Assert.Equal(schema == "interval-only" ? 5 : 1,
                (await repository.LoadAsync()).MonitoringIntervalSeconds);

            var entity = await context.ApplicationSettings.AsNoTracking().SingleAsync();
            Assert.Null(entity.SelectedNetworkAdapter);
            Assert.Null(entity.MonthlyLimitBytes);
            Assert.Null(entity.WarningPercentage);
            Assert.Null(entity.CriticalPercentage);
            await repository.SaveAsync(new ApplicationSettings { MonitoringIntervalSeconds = 10 });
        }

        using var reopened = new DataMonitorContext(options);
        await DatabaseInitializer.InitializeAsync(reopened);
        await DatabaseInitializer.InitializeAsync(reopened);
        Assert.Equal(10, (await new ApplicationSettingsRepository(reopened)
            .LoadAsync()).MonitoringIntervalSeconds);
        Assert.Single(await reopened.ApplicationSettings.ToListAsync());
        var records = await reopened.UsageRecords.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(schema == "fresh" ? 0 : 2, records.Count);
        if (schema != "fresh")
        {
            Assert.Equal("Existing adapter", records[0].AdapterName);
            Assert.Equal(1000, records[0].BytesReceived);
            Assert.Equal(4000, records[1].BytesSent);
            var statistics = new UsageStatisticsService(reopened);
            var summary = await statistics.GetUsageAsync(
                new DateTime(2026, 9, 9), new DateTime(2026, 9, 10));
            Assert.Equal(4000, summary.DownloadBytes);
            Assert.Equal(2000, summary.UploadBytes);
            var points = await statistics.GetChartDataAsync(
                new DateTime(2026, 9, 9), new DateTime(2026, 9, 10));
            Assert.Single(points);
            Assert.Equal(4000 / 1024d / 1024d, points[0].DownloadMB);
        }
    }

    [Fact]
    public async Task SettingsSavePreservesAdapterAndReadsExternalChanges()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DataMonitorContext>().UseSqlite(connection).Options;
        using var context = new DataMonitorContext(options);
        await DatabaseInitializer.InitializeAsync(context);
        var entity = await context.ApplicationSettings.SingleAsync();
        entity.SelectedNetworkAdapter = "reserved-adapter";
        entity.MonthlyLimitBytes = 100_000_000_000;
        entity.WarningPercentage = 80;
        entity.CriticalPercentage = 95;
        await context.SaveChangesAsync();

        var repository = new ApplicationSettingsRepository(context);
        Assert.Equal(1, (await repository.LoadAsync()).MonitoringIntervalSeconds);
        using (var writer = new DataMonitorContext(options))
        {
            await new ApplicationSettingsRepository(writer)
                .SaveAsync(new ApplicationSettings
                {
                    MonitoringIntervalSeconds = 60,
                    SelectedNetworkAdapter = "reserved-adapter",
                    MonthlyLimitBytes = 100_000_000_000,
                    WarningPercentage = 80,
                    CriticalPercentage = 95
                });
        }
        Assert.Equal(60, (await repository.LoadAsync()).MonitoringIntervalSeconds);
        var persisted = await context.ApplicationSettings.AsNoTracking().SingleAsync();
        Assert.Equal("reserved-adapter", persisted.SelectedNetworkAdapter);
        Assert.Equal(100_000_000_000, persisted.MonthlyLimitBytes);
        Assert.Equal(80, persisted.WarningPercentage);
        Assert.Equal(95, persisted.CriticalPercentage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public async Task InvalidIntervalDoesNotOverwriteSavedValue(int interval)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var context = new DataMonitorContext(
            new DbContextOptionsBuilder<DataMonitorContext>().UseSqlite(connection).Options);
        await DatabaseInitializer.InitializeAsync(context);
        var repository = new ApplicationSettingsRepository(context);
        await repository.SaveAsync(new ApplicationSettings
                {
                    MonitoringIntervalSeconds = 60,
                    SelectedNetworkAdapter = "reserved-adapter",
                    MonthlyLimitBytes = 100_000_000_000,
                    WarningPercentage = 80,
                    CriticalPercentage = 95
                });
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            repository.SaveAsync(new ApplicationSettings { MonitoringIntervalSeconds = interval }));
        Assert.Equal(60, (await repository.LoadAsync()).MonitoringIntervalSeconds);
    }
}


