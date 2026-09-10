using DataMonitor.Core.Models;

namespace DataMonitor.Core.Interfaces;

public interface INetworkMonitor
{
    NetworkUsage GetUsage(string? selectedNetworkAdapter = null);
}
