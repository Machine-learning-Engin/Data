using DataMonitor.App.Resources;
using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.Core.Services;
using Microsoft.Extensions.DependencyInjection;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Collections.ObjectModel;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;


namespace DataMonitor.App.ViewModels;


public class DashboardViewModel : INotifyPropertyChanged
{

    private readonly INetworkMonitor _networkMonitor;

    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _monthlyTimer;
    private CancellationTokenSource? _refreshCancellation;
    public MonthlyUsageViewModel MonthlyUsage { get; }


    private NetworkUsage? _previousUsage;
    private readonly IServiceScopeFactory _scopeFactory;
    private string? _selectedAdapter;
    private bool _updatingTraffic;



    private double _downloadMbps;

    private double _uploadMbps;


    private string _liveTraffic = "Idle";
    public string AdapterDisplayName { get; private set; } = "Awaiting sample";
    public string ConnectionText { get; private set; } = "Waiting";
    public string ActivityText => _previousUsage is null ? "Waiting"
        : ConnectionText == "Disconnected" ? "Disconnected"
        : DownloadMbps + UploadMbps <= 0.001 ? "Idle" : "Active";




    public ObservableCollection<TrafficPoint> TrafficHistory { get; }




    public ISeries[] Series { get; }




    public Axis[] XAxes { get; }




    public Axis[] YAxes { get; }






    public double DownloadMbps
    {
        get => _downloadMbps;

        set
        {
            _downloadMbps = value;
            OnPropertyChanged();
        }
    }






    public double UploadMbps
    {
        get => _uploadMbps;

        set
        {
            _uploadMbps = value;
            OnPropertyChanged();
        }
    }






    public string LiveTraffic
    {
        get => _liveTraffic;

        set
        {
            _liveTraffic = value;
            OnPropertyChanged();
        }
    }







    public DashboardViewModel(
        INetworkMonitor networkMonitor,
        MonthlyUsageViewModel monthlyUsage,
        IServiceScopeFactory scopeFactory)
    {

        Console.WriteLine(
            "DashboardViewModel CREATED");


        _networkMonitor = networkMonitor;
        _scopeFactory = scopeFactory;
        MonthlyUsage = monthlyUsage;
        _monthlyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _monthlyTimer.Tick += RefreshMonthlyUsage;



        TrafficHistory =
            new ObservableCollection<TrafficPoint>();





        Series =
        [

            new LineSeries<TrafficPoint>
            {

                Name = "Download",


                Values = TrafficHistory,


                Stroke =
                    new SolidColorPaint(
                        ChartTheme.Color("PrimaryBrush"))
                    {
                        StrokeThickness = 3
                    },


                Fill =
                    new SolidColorPaint(
                        ChartTheme.Color("PrimaryBrush").WithAlpha(50)),



                GeometrySize = 0,


                GeometryStroke =
                    new SolidColorPaint(
                        ChartTheme.Color("TextBrush"))
                    {
                        StrokeThickness = 1
                    },



                Mapping =
                    (point,index) =>
                    new LiveChartsCore.Kernel.Coordinate(
                        index,
                        point.DownloadMbps)

            },






            new LineSeries<TrafficPoint>
            {

                Name = "Upload",


                Values = TrafficHistory,



                Stroke =
                    new SolidColorPaint(
                        ChartTheme.Color("TealBrush"))
                    {
                        StrokeThickness = 3
                    },



                Fill =
                    new SolidColorPaint(
                        ChartTheme.Color("TealBrush").WithAlpha(50)),



                GeometrySize = 0,



                GeometryStroke =
                    new SolidColorPaint(
                        ChartTheme.Color("TextBrush"))
                    {
                        StrokeThickness = 1
                    },



                Mapping =
                    (point,index) =>
                    new LiveChartsCore.Kernel.Coordinate(
                        index,
                        point.UploadMbps)

            }

        ];






        XAxes =
        [

            new Axis
            {

                Name = "Time",


                TextSize = 14,


                LabelsPaint =
                    new SolidColorPaint(
                        ChartTheme.Color("SecondaryTextBrush")),



                NamePaint =
                    new SolidColorPaint(
                        ChartTheme.Color("TextBrush"))

            }

        ];






        YAxes =
        [

            new Axis
            {

                Name = "Mbps",


                MinLimit = 0,


                TextSize = 14,


                LabelsPaint =
                    new SolidColorPaint(
                        ChartTheme.Color("SecondaryTextBrush")),



                NamePaint =
                    new SolidColorPaint(
                        ChartTheme.Color("TextBrush"))

            }

        ];







        _timer =
            new DispatcherTimer();



        _timer.Interval =
            TimeSpan.FromSeconds(1);



        _timer.Tick += UpdateTraffic;



        // View Loaded/Unloaded owns the timer lifetime.

    }








    public async Task StartAsync()
    {
        if (_refreshCancellation is not null) return;
        _refreshCancellation = new CancellationTokenSource();
        _previousUsage = null;

        _timer.Start();
        _monthlyTimer.Start();
        await MonthlyUsage.RefreshAsync(_refreshCancellation.Token);
    }

    public void Stop()
    {
        _timer.Stop();
        _monthlyTimer.Stop();
        _refreshCancellation?.Cancel();
        _refreshCancellation?.Dispose();
        _refreshCancellation = null;
    }

    private async void RefreshMonthlyUsage(object? sender, EventArgs e)
    {
        if (_refreshCancellation is not null)
            await MonthlyUsage.RefreshAsync(_refreshCancellation.Token);
    }

    private async void UpdateTraffic(object? sender, EventArgs e)
    {
        if (_updatingTraffic || _refreshCancellation is null) return;
        var cancellationToken = _refreshCancellation.Token;
        _updatingTraffic = true;
        try
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var settings = await scope.ServiceProvider
                    .GetRequiredService<IApplicationSettingsRepository>().LoadAsync(cancellationToken);
                _selectedAdapter = settings.SelectedNetworkAdapter;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Retaining last adapter selection: {ex.Message}");
            }
            cancellationToken.ThrowIfCancellationRequested();
            var usage = _networkMonitor.GetUsage(_selectedAdapter);
            AdapterDisplayName = usage.AdapterName;
            ConnectionText = usage.CounterSourceId == "adapters:" || usage.AdapterName == "Unknown"
                ? "Disconnected" : "Connected";
            OnPropertyChanged(nameof(AdapterDisplayName));
            OnPropertyChanged(nameof(ConnectionText));
            var speed = _previousUsage is null ? new NetworkSpeed()
                : new NetworkSpeedCalculator().Calculate(_previousUsage, usage);
            DownloadMbps = speed.DownloadBytesPerSecond * 8d / 1024 / 1024;
            UploadMbps = speed.UploadBytesPerSecond * 8d / 1024 / 1024;
            var total = DownloadMbps + UploadMbps;
            LiveTraffic = total <= 0.001 ? "Idle" : $"{total:F2} Mbps";
            if (_previousUsage is not null)
            {
                TrafficHistory.Add(new TrafficPoint
                {
                    Time = usage.Timestamp,
                    DownloadMbps = DownloadMbps,
                    UploadMbps = UploadMbps
                });
                if (TrafficHistory.Count > 60) TrafficHistory.RemoveAt(0);
            }
            _previousUsage = usage;
            OnPropertyChanged(nameof(ActivityText));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _previousUsage = null;
            DownloadMbps = UploadMbps = 0;
            LiveTraffic = $"Network unavailable: {ex.Message}";
            ConnectionText = "Disconnected";
            OnPropertyChanged(nameof(ConnectionText));
            OnPropertyChanged(nameof(ActivityText));
        }
        finally { _updatingTraffic = false; }
    }
    public event PropertyChangedEventHandler?
        PropertyChanged;






    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {

        PropertyChanged?
            .Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));

    }

}



