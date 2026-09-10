using DataMonitor.App.Resources;
using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Services;
using DataMonitor.Core.Models.Statistics;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace DataMonitor.App.ViewModels;


public class HistoryViewModel : INotifyPropertyChanged
{

    private readonly IUsageStatisticsService
        _statisticsService;


    private readonly ObservableCollection<UsageChartPoint>
        _chartPoints = new();


    private string _downloadText = "Loading...";

    private string _uploadText = "Loading...";

    private string _totalText = "Loading...";

    private string _periodTitle = "Today";

    private string _periodRange = "";

    private string _statusText = "Reading usage history...";



    private bool _isLoading;
    private bool _canExport;
    private DateTime _periodStart, _periodEnd;
    public bool CanExport { get => _canExport; private set { _canExport = value; OnPropertyChanged(); } }
    public async Task<string> CreateExportAsync(bool json)
    {
        if (!CanExport) throw new InvalidOperationException("Wait for History to finish loading before exporting.");
        return HistoryExportService.Serialize(await new HistoryExportService(_statisticsService).ReadAsync(_periodStart, _periodEnd), json);
    }
    public void ReportExport(string message) => StatusText = message;

    public bool HasChartData => _chartPoints.Count > 0;

    public HistoryViewModel(
        IUsageStatisticsService statisticsService)
    {

        _chartPoints.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChartData));

        _statisticsService =
            statisticsService;



        Series =
        [
            new LineSeries<UsageChartPoint>
            {
                Name = "Download",

                Values = _chartPoints,

                Stroke =
                    new SolidColorPaint(
                        ChartTheme.Color("PrimaryBrush"))
                    {
                        StrokeThickness = 3
                    },

                Fill =
                    new SolidColorPaint(
                        ChartTheme.Color("PrimaryBrush")
                            .WithAlpha(35)),

                GeometrySize = 5,

                GeometryStroke =
                    new SolidColorPaint(
                        ChartTheme.Color("PrimaryBrush"))
                    {
                        StrokeThickness = 1
                    },

                Mapping =
                    (point, index) =>
                        new LiveChartsCore.Kernel.Coordinate(
                            index,
                            point.DownloadMB)
            },


            new LineSeries<UsageChartPoint>
            {
                Name = "Upload",

                Values = _chartPoints,

                Stroke =
                    new SolidColorPaint(
                        ChartTheme.Color("TealBrush"))
                    {
                        StrokeThickness = 3
                    },

                Fill =
                    new SolidColorPaint(
                        ChartTheme.Color("TealBrush")
                            .WithAlpha(35)),

                GeometrySize = 5,

                GeometryStroke =
                    new SolidColorPaint(
                        ChartTheme.Color("TealBrush"))
                    {
                        StrokeThickness = 1
                    },

                Mapping =
                    (point, index) =>
                        new LiveChartsCore.Kernel.Coordinate(
                            index,
                            point.UploadMB)
            }
        ];



        XAxes =
        [
            new Axis
            {
                Name = "Time",

                TextSize = 13,

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
                Name = "MB",

                MinLimit = 0,

                TextSize = 13,

                LabelsPaint =
                    new SolidColorPaint(
                        ChartTheme.Color("SecondaryTextBrush")),

                NamePaint =
                    new SolidColorPaint(
                        ChartTheme.Color("TextBrush"))
            }
        ];

    }



    public event PropertyChangedEventHandler?
        PropertyChanged;



    public ISeries[] Series { get; }



    public Axis[] XAxes { get; }



    public Axis[] YAxes { get; }



    public string DownloadText
    {
        get => _downloadText;

        private set
        {
            _downloadText = value;

            OnPropertyChanged();
        }
    }



    public string UploadText
    {
        get => _uploadText;

        private set
        {
            _uploadText = value;

            OnPropertyChanged();
        }
    }



    public string TotalText
    {
        get => _totalText;

        private set
        {
            _totalText = value;

            OnPropertyChanged();
        }
    }



    public string PeriodTitle
    {
        get => _periodTitle;

        private set
        {
            _periodTitle = value;

            OnPropertyChanged();
        }
    }



    public string PeriodRange
    {
        get => _periodRange;

        private set
        {
            _periodRange = value;

            OnPropertyChanged();
        }
    }



    public string StatusText
    {
        get => _statusText;

        private set
        {
            _statusText = value;

            OnPropertyChanged();
        }
    }





    public async Task LoadTodayAsync()
    {

        var today =
            DateTime.Now.Date;


        var end =
            DateTime.Now;


        await LoadPeriodAsync(
            today,
            end,
            "Today",
            today.ToString(
                "dddd, MMMM dd, yyyy"),
            ChartAggregation.Hour);

    }





    public async Task LoadWeekAsync()
    {

        var now =
            DateTime.Now;


        var today =
            now.Date;


        var daysSinceMonday =
            ((int)today.DayOfWeek + 6) % 7;


        var start =
            today.AddDays(
                -daysSinceMonday);


        await LoadPeriodAsync(
            start,
            now,
            "This Week",
            $"{start:MMM dd, yyyy} - {now:MMM dd, yyyy}",
            ChartAggregation.Hour);

    }





    public async Task LoadMonthAsync()
    {

        var now =
            DateTime.Now;


        var start =
            new DateTime(
                now.Year,
                now.Month,
                1);


        await LoadPeriodAsync(
            start,
            now,
            "This Month",
            now.ToString(
                "MMMM yyyy"),
            ChartAggregation.Day);

    }





    private async Task LoadPeriodAsync(
        DateTime start,
        DateTime end,
        string title,
        string range,
        ChartAggregation aggregation)
    {
        if (_isLoading) return;
        _isLoading = true;
        CanExport = false;

        try
        {

            SetLoadingState(
                title,
                range);



            var summary =
                await _statisticsService
                    .GetUsageAsync(
                        start,
                        end);



            DownloadText =
                FormatBytes(
                    summary.DownloadBytes);


            UploadText =
                FormatBytes(
                    summary.UploadBytes);


            TotalText =
                FormatBytes(
                    summary.TotalBytes);



            var rawPoints =
                await _statisticsService
                    .GetChartDataAsync(
                        start,
                        end);



            var aggregatedPoints =
                AggregatePoints(
                    rawPoints,
                    aggregation);



            _chartPoints.Clear();



            foreach (var point in aggregatedPoints)
            {

                _chartPoints.Add(
                    point);

            }



            _periodStart = start;
            _periodEnd = end;
            CanExport = true;
            if (summary.TotalBytes <= 0)
            {

                StatusText =
                    "No recorded network usage was found for this period.";

                return;

            }



            if (_chartPoints.Count == 0)
            {

                StatusText =
                    "Usage totals were found, but there are not enough samples to build the chart.";

                return;

            }



            StatusText =
                $"Loaded {_chartPoints.Count} historical chart points from the local database.";

        }
        catch (Exception ex)
        {

            DataMonitor.Infrastructure.Diagnostics.ReleaseLog.Write("History load failed", ex);
            DownloadText =
                "Error";


            UploadText =
                "Error";


            TotalText =
                "Error";


            _chartPoints.Clear();


            StatusText =
                "Could not load History. Reopen History to retry.";

        }
        finally { _isLoading = false; }
    }

    private void SetLoadingState(
        string title,
        string range)
    {

        PeriodTitle =
            title;


        PeriodRange =
            range;


        DownloadText =
            "Loading...";


        UploadText =
            "Loading...";


        TotalText =
            "Loading...";


        StatusText =
            "Reading SQLite usage history...";


        _chartPoints.Clear();

    }





    private static List<UsageChartPoint>
        AggregatePoints(
            List<UsageChartPoint> points,
            ChartAggregation aggregation)
    {
        if (_isLoading) return;
        _isLoading = true;
        CanExport = false;

        if (points.Count == 0)
        {

            return new List<UsageChartPoint>();

        }



        if (aggregation ==
            ChartAggregation.Hour)
        {

            return points
                .GroupBy(
                    x =>
                        new DateTime(
                            x.Time.Year,
                            x.Time.Month,
                            x.Time.Day,
                            x.Time.Hour,
                            0,
                            0))
                .OrderBy(
                    group =>
                        group.Key)
                .Select(
                    group =>
                        new UsageChartPoint
                        {
                            Time =
                                group.Key,

                            DownloadMB =
                                group.Sum(
                                    x =>
                                        x.DownloadMB),

                            UploadMB =
                                group.Sum(
                                    x =>
                                        x.UploadMB)
                        })
                .ToList();

        }



        return points
            .GroupBy(
                x =>
                    x.Time.Date)
            .OrderBy(
                group =>
                    group.Key)
            .Select(
                group =>
                    new UsageChartPoint
                    {
                        Time =
                            group.Key,

                        DownloadMB =
                            group.Sum(
                                x =>
                                    x.DownloadMB),

                        UploadMB =
                            group.Sum(
                                x =>
                                    x.UploadMB)
                    })
            .ToList();

    }





    private static string FormatBytes(
        long bytes)
    {

        if (bytes < 0)
        {
            bytes = 0;
        }



        const double KB =
            1024d;


        const double MB =
            KB * 1024d;


        const double GB =
            MB * 1024d;


        const double TB =
            GB * 1024d;



        if (bytes >= TB)
        {

            return
                $"{bytes / TB:F2} TB";

        }



        if (bytes >= GB)
        {

            return
                $"{bytes / GB:F2} GB";

        }



        if (bytes >= MB)
        {

            return
                $"{bytes / MB:F2} MB";

        }



        if (bytes >= KB)
        {

            return
                $"{bytes / KB:F2} KB";

        }



        return
            $"{bytes} B";

    }





    private void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));

    }





    private enum ChartAggregation
    {

        Hour,

        Day

    }

}


