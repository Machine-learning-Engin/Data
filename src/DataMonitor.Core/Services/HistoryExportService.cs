using System.Globalization;
using System.Text;
using System.Text.Json;
using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.Core.Models.Statistics;

namespace DataMonitor.Core.Services;

public sealed record HistoryExport(UsageSummary Summary, IReadOnlyList<UsageChartPoint> Records);

public sealed class HistoryExportService(IUsageStatisticsService statistics)
{
    public async Task<HistoryExport> ReadAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        if (end < start) throw new ArgumentException("The export end must not precede its start.");
        // Reuse the existing counter-source and reset-aware calculations.
        var summary = await statistics.GetUsageAsync(start, end, cancellationToken);
        var points = await statistics.GetChartDataAsync(start, end, cancellationToken);
        return new(summary, points);
    }

    public static string Serialize(HistoryExport data, bool json)
    {
        if (json) return JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            timeBasis = "Local time, as recorded by DataMonitor",
            megabyteBytes = 1048576,
            summary = data.Summary,
            records = data.Records
        }, new JsonSerializerOptions { WriteIndented = true });

        var csv = new StringBuilder("DateTimeLocal,DownloadMB,UploadMB,TotalMB\r\n");
        foreach (var p in data.Records)
            csv.Append(p.Time.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture)).Append(',')
                .Append(p.DownloadMB.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(p.UploadMB.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(p.TotalMB.ToString("R", CultureInfo.InvariantCulture)).Append("\r\n");
        return csv.ToString();
    }
}
