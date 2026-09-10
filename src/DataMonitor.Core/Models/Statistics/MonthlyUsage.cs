namespace DataMonitor.Core.Models.Statistics;

public class MonthlyUsage
{
    public int Year { get; set; }

    public int Month { get; set; }


    public long DownloadBytes { get; set; }


    public long UploadBytes { get; set; }


    public long TotalBytes =>
        DownloadBytes + UploadBytes;


    public double TotalGB =>
        TotalBytes / 1024d / 1024d / 1024d;
}