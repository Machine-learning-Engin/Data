using DataMonitor.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace DataMonitor.Database;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        DataMonitorContext context,
        CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(
            cancellationToken);

        // Existing installations use EnsureCreated rather than
        // EF migrations. SQLite's write transaction serializes
        // application/worker upgrades.
        //
        // All schema additions are additive. Existing UsageRecords
        // and saved settings are preserved.
        await using var transaction =
            await context.Database.BeginTransactionAsync(
                cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS ApplicationSettings (
                Id INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
                MonitoringIntervalSeconds INTEGER NOT NULL DEFAULT 1
                    CHECK (MonitoringIntervalSeconds BETWEEN 1 AND 60),
                SelectedNetworkAdapter TEXT NULL,
                MonthlyLimitBytes INTEGER NULL,
                WarningPercentage INTEGER NULL,
                CriticalPercentage INTEGER NULL
            );
            """,
            cancellationToken);

        // Always retrieve the current schema after creating the
        // table. This is important because existing installations
        // may have an older version of ApplicationSettings.
        var columns =
            await context.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT name AS Value
                    FROM pragma_table_info('ApplicationSettings')
                    """)
                .ToListAsync(cancellationToken);

        // Additive v1.0 settings upgrade.
        //
        // StartWithWindows and NotificationLevel have safe defaults
        // so existing databases can be upgraded without losing
        // existing settings or requiring destructive table rebuilds.
        var additions =
            new[]
            {
                ("StartWithWindows", "INTEGER NOT NULL DEFAULT 0"),
                ("NotificationMonth", "TEXT NULL"),
                ("NotificationLevel", "INTEGER NOT NULL DEFAULT 0"),
                ("LastMaintenance", "TEXT NULL")
            };

        foreach (var addition in additions)
        {
            if (!columns.Contains(addition.Item1))
            {
                await context.Database.ExecuteSqlRawAsync(
                    $"ALTER TABLE ApplicationSettings " +
                    $"ADD COLUMN {addition.Item1} {addition.Item2};",
                    cancellationToken);
            }
        }

        // Refresh the column list because the previous ALTER TABLE
        // operations may have changed the schema.
        columns =
            await context.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT name AS Value
                    FROM pragma_table_info('ApplicationSettings')
                    """)
                .ToListAsync(cancellationToken);

        // Upgrade databases that were previously shipped with only
        // the monitoring interval setting.
        if (!columns.Contains("SelectedNetworkAdapter"))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE ApplicationSettings
                ADD COLUMN SelectedNetworkAdapter TEXT NULL;
                """,
                cancellationToken);
        }

        if (!columns.Contains("MonthlyLimitBytes"))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE ApplicationSettings
                ADD COLUMN MonthlyLimitBytes INTEGER NULL;
                """,
                cancellationToken);
        }

        if (!columns.Contains("WarningPercentage"))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE ApplicationSettings
                ADD COLUMN WarningPercentage INTEGER NULL;
                """,
                cancellationToken);
        }

        if (!columns.Contains("CriticalPercentage"))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE ApplicationSettings
                ADD COLUMN CriticalPercentage INTEGER NULL;
                """,
                cancellationToken);
        }

        // Ensure the singleton settings row exists.
        //
        // Explicitly provide values for every currently required
        // non-null setting. This is important for databases created
        // from the newer EF model where StartWithWindows and
        // NotificationLevel are NOT NULL.
        //
        // ON CONFLICT ensures that an existing user's settings are
        // never overwritten.
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO ApplicationSettings
                (
                    Id,
                    MonitoringIntervalSeconds,
                    StartWithWindows,
                    NotificationLevel
                )
            VALUES
                (
                    1,
                    1,
                    0,
                    0
                )
            ON CONFLICT(Id) DO NOTHING;
            """,
            cancellationToken);

        // UsageRecords was also upgraded additively when adapter
        // source tracking was introduced.
        var usageColumns =
            await context.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT name AS Value
                    FROM pragma_table_info('UsageRecords')
                    """)
                .ToListAsync(cancellationToken);

        if (!usageColumns.Contains("CounterSourceId"))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE UsageRecords
                ADD COLUMN CounterSourceId TEXT NULL;
                """,
                cancellationToken);
        }

        await transaction.CommitAsync(
            cancellationToken);
    }
}