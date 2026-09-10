using System.Net.NetworkInformation;
using DataMonitor.Core.Models;
using DataMonitor.Core.Services;
using DataMonitor.Infrastructure.Network;

namespace DataMonitor.Infrastructure.Tests;

public sealed class AdapterManagementTests
{
    [Fact]
    public void DiscoveryFiltersConservativelyAndReturnsMetadata()
    {
        var wifi = new FakeInterface("wifi", NetworkInterfaceType.Wireless80211);
        var virtualEthernet = new FakeInterface("vEthernet", NetworkInterfaceType.Ethernet);
        var unknownSpeed = new FakeInterface("unknown-speed", NetworkInterfaceType.Ethernet) { SpeedUnavailable = true };
        var service = new WindowsNetworkAdapterService(() =>
        [
            wifi, virtualEthernet, unknownSpeed,
            new FakeInterface("filter", NetworkInterfaceType.Ethernet)
            {
                AdapterDescription = "Intel(R) Wireless-AC 9560-QoS Packet Scheduler-0000"
            },
            new FakeInterface("down", NetworkInterfaceType.Ethernet) { Status = OperationalStatus.Down },
            new FakeInterface("loopback", NetworkInterfaceType.Loopback),
            new FakeInterface("tunnel", NetworkInterfaceType.Tunnel)
        ]);
        var adapters = service.GetAvailableAdapters();
        Assert.Equal(3, adapters.Count);
        var info = Assert.Single(adapters.Where(x => x.Id == "wifi"));
        Assert.Equal("wifi name", info.Name);
        Assert.Equal("wifi description", info.Description);
        Assert.Equal(NetworkInterfaceType.Wireless80211, info.InterfaceType);
        Assert.Equal(OperationalStatus.Up, info.OperationalStatus);
        Assert.Equal(1_000_000_000, info.SpeedBitsPerSecond);
        Assert.Null(adapters.Single(x => x.Id == "unknown-speed").SpeedBitsPerSecond);
        Assert.Contains(adapters, x => x.Id == "vEthernet");
        Assert.DoesNotContain(adapters, x => x.Id == "filter");
        Assert.Contains(service.GetUsageSnapshots(), x => x.CounterSourceId == "filter");
    }

    [Fact]
    public void SelectedIdAutomaticFallbackAndReconnectUseCorrectCounters()
    {
        var wifi = new FakeInterface("wifi", NetworkInterfaceType.Wireless80211) { Received = 100, Sent = 10 };
        var ethernet = new FakeInterface("ethernet", NetworkInterfaceType.Ethernet) { Received = 900, Sent = 90 };
        var monitor = new WindowsNetworkMonitor(new WindowsNetworkAdapterService(() => [wifi, ethernet]));
        Assert.Equal(1000, monitor.GetUsage().BytesReceived);
        Assert.Equal(100, monitor.GetUsage().BytesSent);
        Assert.Equal(900, monitor.GetUsage("ETHERNET").BytesReceived);
        var selected = monitor.GetUsage("wifi");
        Assert.Equal(100, selected.BytesReceived);
        Assert.Equal("wifi name", selected.AdapterName);
        Assert.Equal(1000, monitor.GetUsage("missing").BytesReceived);

        wifi.Status = OperationalStatus.Down;
        var fallback = monitor.GetUsage("wifi");
        Assert.Equal(900, fallback.BytesReceived);
        Assert.NotEqual(selected.CounterSourceId, fallback.CounterSourceId);
        wifi.Status = OperationalStatus.Up;
        Assert.Equal(selected.CounterSourceId, monitor.GetUsage("wifi").CounterSourceId);
        Assert.Equal(100, monitor.GetUsage("wifi").BytesReceived);
        wifi.CountersUnavailable = true;
        Assert.Equal(900, monitor.GetUsage("wifi").BytesReceived);
        ethernet.Status = OperationalStatus.Down;
        Assert.Equal(0, monitor.GetUsage("wifi").BytesReceived);
    }

    [Fact]
    public void AutomaticSourceIdentityIsStableWhenEnumerationOrderChanges()
    {
        var a = new FakeInterface("a", NetworkInterfaceType.Ethernet);
        var b = new FakeInterface("b", NetworkInterfaceType.Ethernet);
        NetworkInterface[] interfaces = [a, b];
        var monitor = new WindowsNetworkMonitor(new WindowsNetworkAdapterService(() => interfaces));
        var first = monitor.GetUsage();
        interfaces = [b, a];
        Assert.Equal(first.CounterSourceId, monitor.GetUsage().CounterSourceId);
    }

    [Fact]
    public void SpeedDoesNotSpikeAtCounterSourceBoundary()
    {
        var previous = new NetworkUsage
        {
            Timestamp = DateTime.Today, CounterSourceId = "a", BytesReceived = 100, BytesSent = 100
        };
        var current = new NetworkUsage
        {
            Timestamp = DateTime.Today.AddSeconds(1), CounterSourceId = "b",
            BytesReceived = 1_000_000, BytesSent = 2_000_000
        };
        var speed = new NetworkSpeedCalculator().Calculate(previous, current);
        Assert.Equal(0, speed.DownloadBytesPerSecond);
        Assert.Equal(0, speed.UploadBytesPerSecond);
    }

    private sealed class FakeInterface(string id, NetworkInterfaceType type) : NetworkInterface
    {
        public OperationalStatus Status { get; set; } = OperationalStatus.Up;
        public bool SpeedUnavailable { get; set; }
        public bool CountersUnavailable { get; set; }
        public long Received { get; set; }
        public long Sent { get; set; }
        public override string Id => id;
        public override string Name => id + " name";
        public string AdapterDescription { get; set; } = id + " description";
        public override string Description => AdapterDescription;
        public override NetworkInterfaceType NetworkInterfaceType => type;
        public override OperationalStatus OperationalStatus => Status;
        public override long Speed => SpeedUnavailable ? throw new NetworkInformationException() : 1_000_000_000;
        public override IPv4InterfaceStatistics GetIPv4Statistics() =>
            CountersUnavailable ? throw new NetworkInformationException() : new Counters(Received, Sent);
    }

    private sealed class Counters(long received, long sent) : IPv4InterfaceStatistics
    {
        public override long BytesReceived => received;
        public override long BytesSent => sent;
        public override long IncomingPacketsDiscarded => 0;
        public override long IncomingPacketsWithErrors => 0;
        public override long IncomingUnknownProtocolPackets => 0;
        public override long NonUnicastPacketsReceived => 0;
        public override long NonUnicastPacketsSent => 0;
        public override long OutgoingPacketsDiscarded => 0;
        public override long OutgoingPacketsWithErrors => 0;
        public override long OutputQueueLength => 0;
        public override long UnicastPacketsReceived => 0;
        public override long UnicastPacketsSent => 0;
    }
}

