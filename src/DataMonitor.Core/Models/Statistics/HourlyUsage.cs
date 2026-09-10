namespace DataMonitor.Core.Models.Statistics;

public class HourlyUsage
{
    public DateTime Hour { get; set; }


    public long DownloadBytes { get; set; }


    public long UploadBytes { get; set; }


    public long TotalBytes =>
        DownloadBytes + UploadBytes;
}