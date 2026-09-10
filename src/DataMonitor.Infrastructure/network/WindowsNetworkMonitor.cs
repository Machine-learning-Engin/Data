using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;

namespace DataMonitor.Infrastructure.Network;

public sealed class WindowsNetworkMonitor(INetworkAdapterService adapters) : INetworkMonitor
{
    public NetworkUsage GetUsage(string? selectedNetworkAdapter = null)
    {
        var snapshots = adapters.GetUsageSnapshots();
        var selected = string.IsNullOrWhiteSpace(selectedNetworkAdapter) ? null
            : snapshots.FirstOrDefault(x => string.Equals(
                x.CounterSourceId, selectedNetworkAdapter, StringComparison.OrdinalIgnoreCase));

        // An unavailable saved adapter falls back without changing the saved preference.
        var monitored = selected is null ? snapshots : new[] { selected };
        return new NetworkUsage
        {
            Timestamp = DateTime.Now,
            AdapterName = selected?.AdapterName ?? snapshots.FirstOrDefault()?.AdapterName ?? "Unknown",
            CounterSourceId = "adapters:" + string.Join("|", monitored
                .Select(x => x.CounterSourceId!.ToUpperInvariant()).OrderBy(x => x, StringComparer.Ordinal)),
            BytesReceived = monitored.Sum(x => x.BytesReceived),
            BytesSent = monitored.Sum(x => x.BytesSent)
        };
    }
}
