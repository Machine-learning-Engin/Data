namespace DataMonitor.Core.Models;

public class UsageSummary
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public long DownloadBytes { get; set; }

    public long UploadBytes { get; set; }


    public long TotalBytes =>
        DownloadBytes + UploadBytes;


    public double DownloadGigabytes =>
        DownloadBytes / 1024d / 1024d / 1024d;


    public double UploadGigabytes =>
        UploadBytes / 1024d / 1024d / 1024d;


    public double TotalGigabytes =>
        TotalBytes / 1024d / 1024d / 1024d;
}