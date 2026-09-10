using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DataMonitor.App.ViewModels;
using DataMonitor.App.Views;
using DataMonitor.App.Services;
using DataMonitor.Core.Interfaces;
using DataMonitor.Database;
using DataMonitor.Database.Data;
using DataMonitor.Database.Services;
using DataMonitor.Infrastructure.Network;
using DataMonitor.Infrastructure.Windows;
using DataMonitor.Infrastructure.Diagnostics;
using DataMonitor.MonitorService;

namespace DataMonitor.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private Mutex? _instance;
    private bool _ownsInstance;
    private Worker? _worker;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            ReleaseLog.Write("Unexpected UI failure", args.Exception);
            MessageBox.Show("DataMonitor encountered an unexpected error and must close. Please reopen it. Details were saved in the application log.",
                "DataMonitor", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };
        try
        {
            _instance = new Mutex(false, @"Local\DataMonitor.UI.v1");
            try { _ownsInstance = _instance.WaitOne(0); }
            catch (AbandonedMutexException) { _ownsInstance = true; }
            if (!_ownsInstance)
            {
                MessageBox.Show("DataMonitor is already running. Use its existing window.", "DataMonitor");
                Shutdown();
                return;
            }

            var services = new ServiceCollection();
            var databasePath = DatabasePath.GetDatabasePath();
            services.AddLogging(b => b.AddProvider(new ReleaseLoggerProvider()));
            services.AddDbContext<DataMonitorContext>(options => options.UseSqlite($"Data Source={databasePath}"));
            services.AddSingleton<INetworkAdapterService, WindowsNetworkAdapterService>();
            services.AddSingleton<INetworkMonitor, WindowsNetworkMonitor>();
            services.AddScoped<IUsageRepository, UsageRepository>();
            services.AddScoped<IUsageStatisticsService, UsageStatisticsService>();
            services.AddScoped<IApplicationSettingsRepository, ApplicationSettingsRepository>();
            services.AddScoped<IUsageAlertService, UsageAlertService>();
            services.AddScoped<IDatabaseMaintenanceService, DatabaseMaintenanceService>();
            services.AddScoped<UsageNotificationService>();
            services.AddSingleton<IUsageNotificationSink, WindowsUsageNotificationSink>();
            services.AddSingleton<IStartupRegistration>(new WindowsStartupRegistration(Environment.ProcessPath!));
            services.AddSingleton(new MonitoringLease(databasePath + ".monitor.lock"));
            services.AddSingleton<Worker>();
            services.AddSingleton<ReleaseCoordinator>();
            services.AddTransient<MonthlyUsageViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<HistoryViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<DashboardView>();
            services.AddTransient<HistoryView>();
            services.AddTransient<SettingsView>();
            services.AddTransient<MainWindow>();
            _serviceProvider = services.BuildServiceProvider();
            using (var scope = _serviceProvider.CreateScope())
            {
                await DatabaseInitializer.InitializeAsync(scope.ServiceProvider.GetRequiredService<DataMonitorContext>());
                var settings = await scope.ServiceProvider.GetRequiredService<IApplicationSettingsRepository>().LoadAsync();
                try { scope.ServiceProvider.GetRequiredService<IStartupRegistration>().SetEnabled(settings.StartWithWindows); }
                catch (Exception ex)
                {
                    ReleaseLog.Write("Startup registration could not be synchronized", ex);
                    MessageBox.Show("Windows startup registration could not be updated. You can retry by saving Settings.", "DataMonitor");
                }
            }
            _worker = _serviceProvider.GetRequiredService<Worker>();
            await _worker.StartAsync(CancellationToken.None);
            _serviceProvider.GetRequiredService<ReleaseCoordinator>().Start();
            var window = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            ReleaseLog.Write("Application startup failed", ex);
            MessageBox.Show("DataMonitor could not start. Close other copies and try again. Check access to your application data folder. Details were saved in the application log.",
                "DataMonitor", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.GetService<ReleaseCoordinator>()?.Dispose();
        if (_worker is not null)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try { _worker.StopAsync(timeout.Token).GetAwaiter().GetResult(); }
            catch (Exception ex) { ReleaseLog.Write("Monitoring shutdown", ex); }
        }
        _serviceProvider?.Dispose();
        if (_ownsInstance) _instance?.ReleaseMutex();
        _instance?.Dispose();
        base.OnExit(e);
    }
}
