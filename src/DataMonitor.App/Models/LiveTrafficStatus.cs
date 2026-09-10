namespace DataMonitor.App.Models;

public class LiveTrafficStatus
{
    public double DownloadMbps { get; set; }

    public double UploadMbps { get; set; }


    public double TotalMbps =>
        DownloadMbps + UploadMbps;


    public double DownloadBytesPerSecond { get; set; }

    public double UploadBytesPerSecond { get; set; }
}