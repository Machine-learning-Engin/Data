using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.Database.Data;
using DataMonitor.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataMonitor.Database;

public class UsageRepository : IUsageRepository
{
    private readonly DataMonitorContext _context;

    public UsageRepository(DataMonitorContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(
        NetworkUsage usage,
        CancellationToken cancellationToken = default)
    {
        var record = new UsageRecord
        {
            Timestamp = usage.Timestamp,
            AdapterName = usage.AdapterName,
            CounterSourceId = usage.CounterSourceId,
            BytesReceived = usage.BytesReceived,
            BytesSent = usage.BytesSent
        };

        _context.UsageRecords.Add(record);

        await _context.SaveChangesAsync(
            cancellationToken);
    }
}
