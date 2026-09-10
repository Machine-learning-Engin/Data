using DataMonitor.Core.Models;
using DataMonitor.Core.Models.Statistics;


namespace DataMonitor.Core.Interfaces;


public interface IUsageStatisticsService
{

    Task<UsageSummary> GetUsageAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default);



    Task<DailyUsage> GetDailyUsageAsync(
        DateTime date,
        CancellationToken cancellationToken = default);



    Task<MonthlyUsage> GetMonthlyUsageAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);



    Task<List<UsageChartPoint>> GetChartDataAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default);

}