using System.Net.NetworkInformation;

namespace DataMonitor.Infrastructure.Network;

public static class NetworkAdapterDiagnostics
{
    public static void PrintAdapters()
    {
        Console.WriteLine();
        Console.WriteLine("NETWORK ADAPTERS");
        Console.WriteLine("================");

        foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
        {
            Console.WriteLine($"Name: {network.Name}");
            Console.WriteLine($"Description: {network.Description}");
            Console.WriteLine($"Type: {network.NetworkInterfaceType}");
            Console.WriteLine($"Status: {network.OperationalStatus}");
            Console.WriteLine($"Speed: {network.Speed:N0} bps");
            Console.WriteLine("----------------");
        }
    }
}