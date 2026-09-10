using DataMonitor.Core.Models;

namespace DataMonitor.Core.Services;

public class NetworkSpeedCalculator
{
    public NetworkSpeed Calculate(
        NetworkUsage previous,
        NetworkUsage current)
    {
        if (!string.Equals(previous.CounterSourceId, current.CounterSourceId, StringComparison.Ordinal))
            return new NetworkSpeed();

        var elapsedSeconds =
            (current.Timestamp - previous.Timestamp).TotalSeconds;

        if (elapsedSeconds <= 0)
        {
            return new NetworkSpeed();
        }

        var downloadDifference =
            current.BytesReceived - previous.BytesReceived;

        var uploadDifference =
            current.BytesSent - previous.BytesSent;

        if (downloadDifference < 0)
        {
            downloadDifference = 0;
        }

        if (uploadDifference < 0)
        {
            uploadDifference = 0;
        }

        return new NetworkSpeed
        {
            DownloadBytesPerSecond =
                downloadDifference / elapsedSeconds,

            UploadBytesPerSecond =
                uploadDifference / elapsedSeconds
        };
    }
}
