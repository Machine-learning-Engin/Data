using DataMonitor.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace DataMonitor.Database;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        DataMonitorContext context,
        CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken);

        // Existing installations use EnsureCreated, not EF migrations.
        // SQLite's write transaction serializes App/Worker upgrades. All additions
        // and the singleton seed commit together without rebuilding usage tables.
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ApplicationSettings (
                Id INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
                MonitoringIntervalSeconds INTEGER NOT NULL DEFAULT 1
                    CHECK (MonitoringIntervalSeconds BETWEEN 1 AND 60),
                SelectedNetworkAdapter TEXT NULL,
                MonthlyLimitBytes INTEGER NULL,
                WarningPercentage INTEGER NULL,
                CriticalPercentage INTEGER NULL
            );
            """, cancellationToken);

        var columns = await context.Database.SqlQueryRaw<string>("""
            SELECT name AS Value FROM pragma_table_info('ApplicationSettings')
            """).ToListAsync(cancellationToken);

        // Additive v1.0 upgrade: never rebuild or relocate the usage table.
        foreach (var addition in new[] {
            ("StartWithWindows", "INTEGER NOT NULL DEFAULT 0"),
            ("NotificationMonth", "TEXT NULL"),
            ("NotificationLevel", "INTEGER NOT NULL DEFAULT 0"),
            ("LastMaintenance", "TEXT NULL") })
        {
            if (!columns.Contains(addition.Item1))
                await context.Database.ExecuteSqlRawAsync(
                    $"ALTER TABLE ApplicationSettings ADD COLUMN {addition.Item1} {addition.Item2};",
                    cancellationToken);
        }

        // Upgrade the previously shipped interval-only table in place.
        if (!columns.Contains("SelectedNetworkAdapter"))
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE ApplicationSettings ADD COLUMN SelectedNetworkAdapter TEXT NULL;",
                cancellationToken);
        if (!columns.Contains("MonthlyLimitBytes"))
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE ApplicationSettings ADD COLUMN MonthlyLimitBytes INTEGER NULL;",
                cancellationToken);
        if (!columns.Contains("WarningPercentage"))
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE ApplicationSettings ADD COLUMN WarningPercentage INTEGER NULL;",
                cancellationToken);
        if (!columns.Contains("CriticalPercentage"))
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE ApplicationSettings ADD COLUMN CriticalPercentage INTEGER NULL;",
                cancellationToken);

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO ApplicationSettings (Id, MonitoringIntervalSeconds)
            VALUES (1, 1) ON CONFLICT(Id) DO NOTHING;
            """, cancellationToken);

        var usageColumns = await context.Database.SqlQueryRaw<string>("""
            SELECT name AS Value FROM pragma_table_info('UsageRecords')
            """).ToListAsync(cancellationToken);
        if (!usageColumns.Contains("CounterSourceId"))
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE UsageRecords ADD COLUMN CounterSourceId TEXT NULL;",
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}


