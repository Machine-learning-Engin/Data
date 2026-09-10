using System.Net.NetworkInformation;

namespace DataMonitor.Core.Models;

public sealed class NetworkAdapterInfo
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public NetworkInterfaceType InterfaceType { get; init; }
    public OperationalStatus OperationalStatus { get; init; }
    public long? SpeedBitsPerSecond { get; init; }
    public string DisplayName => Id.Length == 0 ? Name
        : $"{Name} — {Description} ({InterfaceType}, {OperationalStatus}" +
          (SpeedBitsPerSecond is > 0 ? $", {SpeedBitsPerSecond.Value / 1_000_000d:0.##} Mbps)" : ")");
}
