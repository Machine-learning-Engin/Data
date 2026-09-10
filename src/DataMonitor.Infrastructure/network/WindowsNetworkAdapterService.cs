using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;

namespace DataMonitor.Infrastructure.Network;

public sealed class WindowsNetworkAdapterService : INetworkAdapterService
{
    private readonly Func<NetworkInterface[]> _getInterfaces;

    public WindowsNetworkAdapterService() : this(NetworkInterface.GetAllNetworkInterfaces) { }

    // Allows deterministic discovery/counter tests without changing machine adapters.
    public WindowsNetworkAdapterService(Func<NetworkInterface[]> getInterfaces)
    {
        _getInterfaces = getInterfaces;
    }

    public IReadOnlyList<NetworkAdapterInfo> GetAvailableAdapters()
    {
        var adapters = new List<NetworkAdapterInfo>();
        foreach (var network in _getInterfaces())
        {
            try
            {
                if (!IsUsable(network) || IsFilterLayer(network.Description)) continue;
                long? speed = null;
                try { speed = network.Speed > 0 ? network.Speed : null; }
                catch (NetworkInformationException) { }
                catch (NotSupportedException) { }
                adapters.Add(new NetworkAdapterInfo
                {
                    Id = network.Id,
                    Name = network.Name,
                    Description = network.Description,
                    InterfaceType = network.NetworkInterfaceType,
                    OperationalStatus = network.OperationalStatus,
                    SpeedBitsPerSecond = speed
                });
            }
            catch (NetworkInformationException)
            {
                // An interface can disappear during enumeration.
            }
        }
        return adapters.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public IReadOnlyList<NetworkUsage> GetUsageSnapshots()
    {
        var snapshots = new List<NetworkUsage>();
        foreach (var network in _getInterfaces())
        {
            try
            {
                if (!IsUsable(network)) continue;
                var counters = network.GetIPv4Statistics();
                snapshots.Add(new NetworkUsage
                {
                    Timestamp = DateTime.Now,
                    AdapterName = network.Name,
                    CounterSourceId = network.Id,
                    BytesReceived = counters.BytesReceived,
                    BytesSent = counters.BytesSent
                });
            }
            catch (NetworkInformationException) { }
            catch (NotSupportedException) { }
        }
        return snapshots;
    }

    private static bool IsFilterLayer(string description) =>
        Regex.IsMatch(description,
            @"-(QoS Packet Scheduler|Native WiFi Filter Driver|Virtual WiFi Filter Driver|WFP 802\.3 MAC Layer LightWeight Filter|WFP Native MAC Layer LightWeight Filter)-[0-9]+$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool IsUsable(NetworkInterface network) =>
        network.OperationalStatus == OperationalStatus.Up &&
        network.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
        network.NetworkInterfaceType != NetworkInterfaceType.Tunnel;
    // Keep active VPN, Hyper-V and other Ethernet-like virtual interfaces:
    // name-based exclusion could remove the user's actual network route.
}

