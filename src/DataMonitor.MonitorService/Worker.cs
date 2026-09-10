using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DataMonitor.MonitorService;

public class Worker : BackgroundService
{
    private readonly INetworkMonitor _networkMonitor;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<Worker> _logger;


    private DateTime _nextMaintenance = DateTime.MinValue;
    private NetworkUsage? _previousUsage;
    private string? _selectedNetworkAdapter;



    public Worker(
        INetworkMonitor networkMonitor,
        IServiceScopeFactory scopeFactory,
        ILogger<Worker> logger)
    {
        _networkMonitor = networkMonitor;

        _scopeFactory = scopeFactory;
        _logger = logger;
    }




    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var lastDatabaseSave =
            DateTime.UtcNow;


        var lastStatisticsPrint =
            DateTime.UtcNow;



        var intervalSeconds = 1;
        int? reportedInterval = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var lease = scope.ServiceProvider.GetService<MonitoringLease>();
                    if (lease is not null && !lease.TryAcquire())
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }
                    // The service checks its persisted last-run time; the Worker
                    // consults it only hourly, never on every sampling cycle.
                    if (DateTime.UtcNow >= _nextMaintenance)
                    {
                        _nextMaintenance = DateTime.UtcNow.AddHours(1);
                        try
                        {
                            var maintenance = scope.ServiceProvider.GetService<IDatabaseMaintenanceService>();
                            if (maintenance is not null) await maintenance.RunIfDueAsync(DateTime.Now, stoppingToken);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        { _logger.LogWarning(ex, "Database maintenance failed; monitoring will continue."); }
                    }
                }
            intervalSeconds = await ReadIntervalAsync(intervalSeconds, stoppingToken);
            if (reportedInterval != intervalSeconds)
            {
                _logger.LogInformation("Monitoring interval: {IntervalSeconds} seconds", intervalSeconds);
                reportedInterval = intervalSeconds;
            }

            var currentUsage =
                _networkMonitor.GetUsage(_selectedNetworkAdapter);



            // ----------------------------
            // Live speed calculation
            // ----------------------------

            if (_previousUsage != null)
            {
                var calculator =
                    new NetworkSpeedCalculator();


                var speed =
                    calculator.Calculate(
                        _previousUsage,
                        currentUsage);



                Console.WriteLine(
                    $"Adapter: {currentUsage.AdapterName}"
                );


                Console.WriteLine(
                    $"Download: {speed.DownloadMegabitsPerSecond:F2} Mbps"
                );


                Console.WriteLine(
                    $"Upload: {speed.UploadMegabitsPerSecond:F2} Mbps"
                );


                Console.WriteLine(
                    $"Download: {speed.DownloadBytesPerSecond:F2} B/s"
                );


                Console.WriteLine(
                    $"Upload: {speed.UploadBytesPerSecond:F2} B/s"
                );


                Console.WriteLine(
                    "----------------------"
                );
            }




            // ----------------------------
            // Save database snapshot
            // at the first sample after 5 seconds have elapsed
            // ----------------------------

            if ((DateTime.UtcNow - lastDatabaseSave)
                .TotalSeconds >= 5 ||
                (_previousUsage is not null &&
                 !string.Equals(_previousUsage.CounterSourceId, currentUsage.CounterSourceId, StringComparison.Ordinal)))
            {
                await SaveUsageAsync(
                    currentUsage,
                    stoppingToken);



                lastDatabaseSave =
                    DateTime.UtcNow;
            }




            // ----------------------------
            // Statistics every 30 seconds
            // ----------------------------

            if ((DateTime.UtcNow - lastStatisticsPrint)
                .TotalSeconds >= 30)
            {
                using var scope =
                    _scopeFactory.CreateScope();



                var statistics =
                    scope.ServiceProvider
                    .GetRequiredService<IUsageStatisticsService>();



                var today =
                    await statistics.GetDailyUsageAsync(
                        DateTime.Today,
                        stoppingToken);



                var month =
                    await statistics.GetMonthlyUsageAsync(
                        DateTime.Today.Year,
                        DateTime.Today.Month,
                        stoppingToken);



                Console.WriteLine();


                Console.WriteLine(
                    "========= DAILY USAGE ========="
                );


                Console.WriteLine(
                    $"Download: {today.DownloadGB:F3} GB"
                );


                Console.WriteLine(
                    $"Upload: {today.UploadGB:F3} GB"
                );


                Console.WriteLine(
                    $"Total: {today.TotalGB:F3} GB"
                );


                Console.WriteLine(
                    "==============================="
                );



                Console.WriteLine();



                Console.WriteLine(
                    "========= MONTH USAGE ========="
                );


                Console.WriteLine(
                    $"Total: {month.TotalGB:F3} GB"
                );


                Console.WriteLine(
                    "==============================="
                );



                lastStatisticsPrint =
                    DateTime.UtcNow;
            }



            _previousUsage =
                currentUsage;



            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Monitoring sample failed; retrying after the configured interval.");
                _previousUsage = null;
            }
            await Task.Delay(
                TimeSpan.FromSeconds(intervalSeconds),
                stoppingToken);
        }
    }






    private async Task<int> ReadIntervalAsync(int previousInterval, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = await scope.ServiceProvider
                .GetRequiredService<IApplicationSettingsRepository>()
                .LoadAsync(cancellationToken);
            settings.Validate();
            _selectedNetworkAdapter = settings.SelectedNetworkAdapter;
            return settings.MonitoringIntervalSeconds;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Could not read monitoring settings; retaining {IntervalSeconds} seconds.",
                previousInterval);
            return previousInterval;
        }
    }

    private async Task SaveUsageAsync(
        NetworkUsage usage,
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();



        var repository =
            scope.ServiceProvider
            .GetRequiredService<IUsageRepository>();



        await repository.SaveAsync(
            usage,
            cancellationToken);
    }
}



