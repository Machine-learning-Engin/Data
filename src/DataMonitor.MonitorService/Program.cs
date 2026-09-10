using DataMonitor.Core.Interfaces;
using DataMonitor.Database;
using DataMonitor.Database.Data;
using DataMonitor.Infrastructure.Network;
using DataMonitor.MonitorService;
using DataMonitor.Database.Services;

using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var databasePath = DatabasePath.GetDatabasePath();

builder.Services.AddDbContext<DataMonitorContext>(
    options =>
        options.UseSqlite(
            $"Data Source={databasePath}"
        )
);

builder.Services.AddSingleton<
    INetworkMonitor,
    WindowsNetworkMonitor
>();

builder.Services.AddScoped<
    IUsageRepository,
    UsageRepository
>();

builder.Services.AddScoped<
    IUsageStatisticsService,
    UsageStatisticsService>();

builder.Services.AddScoped<IApplicationSettingsRepository, ApplicationSettingsRepository>();

builder.Services.AddSingleton<INetworkAdapterService, WindowsNetworkAdapterService>();

builder.Services.AddScoped<IDatabaseMaintenanceService, DatabaseMaintenanceService>();
builder.Services.AddSingleton(new MonitoringLease(databasePath + ".monitor.lock"));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
            .GetRequiredService<DataMonitorContext>();

    await DatabaseInitializer.InitializeAsync(context);
}

await host.RunAsync();


