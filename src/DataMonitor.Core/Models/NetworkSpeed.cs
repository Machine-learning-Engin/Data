namespace DataMonitor.Core.Models;

public class NetworkSpeed
{
    public double DownloadBytesPerSecond { get; set; }

    public double UploadBytesPerSecond { get; set; }

    public double DownloadBitsPerSecond =>
        DownloadBytesPerSecond * 8;

    public double UploadBitsPerSecond =>
        UploadBytesPerSecond * 8;

    public double DownloadMegabitsPerSecond =>
        DownloadBitsPerSecond / 1_000_000;

    public double UploadMegabitsPerSecond =>
        UploadBitsPerSecond / 1_000_000;
}