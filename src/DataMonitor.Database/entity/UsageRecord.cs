namespace DataMonitor.Database.Entities;

public class UsageRecord
{
    public long Id { get; set; }

    public DateTime Timestamp { get; set; }

    public string AdapterName { get; set; } = string.Empty;

    public string? CounterSourceId { get; set; }

    public long BytesReceived { get; set; }

    public long BytesSent { get; set; }
}
