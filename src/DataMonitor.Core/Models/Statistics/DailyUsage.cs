namespace DataMonitor.Core.Models.Statistics;

public class DailyUsage
{
    public DateTime Date { get; set; }

    public long DownloadBytes { get; set; }

    public long UploadBytes { get; set; }


    public long TotalBytes =>
        DownloadBytes + UploadBytes;


    public double DownloadGB =>
        DownloadBytes / 1024d / 1024d / 1024d;


    public double UploadGB =>
        UploadBytes / 1024d / 1024d / 1024d;


    public double TotalGB =>
        TotalBytes / 1024d / 1024d / 1024d;
}