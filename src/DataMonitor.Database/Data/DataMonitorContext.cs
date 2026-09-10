using DataMonitor.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataMonitor.Database.Data;

public class DataMonitorContext : DbContext
{
    public DataMonitorContext(DbContextOptions<DataMonitorContext> options)
        : base(options)
    {
    }

    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    public DbSet<ApplicationSettingsEntity> ApplicationSettings => Set<ApplicationSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UsageRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AdapterName).IsRequired().HasMaxLength(255);
            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => x.AdapterName);
        });

        modelBuilder.Entity<ApplicationSettingsEntity>(entity =>
        {
            entity.ToTable("ApplicationSettings", table =>
            {
                table.HasCheckConstraint("CK_ApplicationSettings_Singleton", "Id = 1");
                table.HasCheckConstraint("CK_ApplicationSettings_Interval",
                    "MonitoringIntervalSeconds BETWEEN 1 AND 60");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.MonitoringIntervalSeconds).HasDefaultValue(1);
        });
    }
}
