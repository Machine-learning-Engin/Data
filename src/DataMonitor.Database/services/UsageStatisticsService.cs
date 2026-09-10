using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.Core.Models.Statistics;
using DataMonitor.Database.Data;

using Microsoft.EntityFrameworkCore;


namespace DataMonitor.Database.Services;


public class UsageStatisticsService : IUsageStatisticsService
{

    private readonly DataMonitorContext _context;


    public UsageStatisticsService(
        DataMonitorContext context)
    {
        _context = context;
    }



    public async Task<UsageSummary> GetUsageAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {

        var records =
            await _context.UsageRecords
                .Where(x =>
                    x.Timestamp >= start &&
                    x.Timestamp <= end)
                .OrderBy(x => x.Timestamp)
                .ToListAsync(cancellationToken);



        if (records.Count < 2)
        {

            return new UsageSummary
            {
                StartDate = start,
                EndDate = end
            };

        }



        long downloadBytes = 0;

        long uploadBytes = 0;



        for (int i = 1; i < records.Count; i++)
        {

            var previous =
                records[i - 1];


            var current =
                records[i];

            // Never subtract counters from different adapters or adapter sets.
            // Two legacy null IDs retain the original History behavior.
            if (!string.Equals(previous.CounterSourceId, current.CounterSourceId, StringComparison.Ordinal))
                continue;



            var downloadDifference =
                current.BytesReceived -
                previous.BytesReceived;



            var uploadDifference =
                current.BytesSent -
                previous.BytesSent;



            // Ignore counter resets

            if (downloadDifference > 0)
            {
                downloadBytes += downloadDifference;
            }


            if (uploadDifference > 0)
            {
                uploadBytes += uploadDifference;
            }

        }



        return new UsageSummary
        {
            StartDate = start,

            EndDate = end,

            DownloadBytes = downloadBytes,

            UploadBytes = uploadBytes
        };

    }





    public async Task<DailyUsage> GetDailyUsageAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {

        var start =
            date.Date;



        var end =
            date.Date
                .AddDays(1)
                .AddTicks(-1);



        var summary =
            await GetUsageAsync(
                start,
                end,
                cancellationToken);



        return new DailyUsage
        {
            Date = date.Date,

            DownloadBytes =
                summary.DownloadBytes,

            UploadBytes =
                summary.UploadBytes
        };

    }





    public async Task<MonthlyUsage> GetMonthlyUsageAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {

        var start =
            new DateTime(
                year,
                month,
                1);



        var end =
            start
                .AddMonths(1)
                .AddTicks(-1);



        var summary =
            await GetUsageAsync(
                start,
                end,
                cancellationToken);



        return new MonthlyUsage
        {
            Year = year,

            Month = month,

            DownloadBytes =
                summary.DownloadBytes,

            UploadBytes =
                summary.UploadBytes
        };

    }





    public async Task<List<UsageChartPoint>> GetChartDataAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {

        var records =
            await _context.UsageRecords
                .Where(x =>
                    x.Timestamp >= start &&
                    x.Timestamp <= end)
                .OrderBy(x => x.Timestamp)
                .ToListAsync(cancellationToken);



        var points =
            new List<UsageChartPoint>();



        if (records.Count < 2)
        {
            return points;
        }



        for (int i = 1; i < records.Count; i++)
        {

            var previous =
                records[i - 1];


            var current =
                records[i];

            // Never subtract counters from different adapters or adapter sets.
            // Two legacy null IDs retain the original History behavior.
            if (!string.Equals(previous.CounterSourceId, current.CounterSourceId, StringComparison.Ordinal))
                continue;



            var downloadDifference =
                current.BytesReceived -
                previous.BytesReceived;



            var uploadDifference =
                current.BytesSent -
                previous.BytesSent;



            // Ignore network counter resets

            if (downloadDifference < 0)
            {
                downloadDifference = 0;
            }


            if (uploadDifference < 0)
            {
                uploadDifference = 0;
            }



            points.Add(
                new UsageChartPoint
                {

                    Time =
                        current.Timestamp,


                    DownloadMB =
                        downloadDifference /
                        1024d /
                        1024d,


                    UploadMB =
                        uploadDifference /
                        1024d /
                        1024d

                });

        }



        return points;

    }

}
