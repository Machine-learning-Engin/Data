namespace DataMonitor.Core.Models.Statistics;


public class UsageChartPoint
{

    public DateTime Time { get; set; }


    public double DownloadMB { get; set; }


    public double UploadMB { get; set; }


    public double TotalMB =>
        DownloadMB + UploadMB;

}