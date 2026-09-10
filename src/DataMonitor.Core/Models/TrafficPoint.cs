namespace DataMonitor.Core.Models;

public class TrafficPoint
{
    public DateTime Time { get; set; }

    public double DownloadMbps { get; set; }

    public double UploadMbps { get; set; }
}