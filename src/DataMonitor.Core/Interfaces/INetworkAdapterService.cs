using DataMonitor.Core.Models;

namespace DataMonitor.Core.Interfaces;

public interface INetworkAdapterService
{
    IReadOnlyList<NetworkAdapterInfo> GetAvailableAdapters();
    // One raw counter snapshot per currently usable adapter; source ID is its interface ID.
    IReadOnlyList<NetworkUsage> GetUsageSnapshots();
}
