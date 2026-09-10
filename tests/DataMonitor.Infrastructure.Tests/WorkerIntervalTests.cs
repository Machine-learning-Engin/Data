using System.Diagnostics;
using System.Threading.Channels;
using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using DataMonitor.MonitorService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DataMonitor.Infrastructure.Tests;

public sealed class WorkerIntervalTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkerUsesIntervalAndReloadsWithoutRestart(bool failReload)
    {
        var settings = new ChangingSettings(failReload);
        var monitor = new RecordingMonitor();
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationSettingsRepository>(settings);
        services.AddSingleton<IUsageRepository, DiscardUsage>();
        using var provider = services.BuildServiceProvider();
        using var worker = new Worker(monitor,
            provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<Worker>.Instance);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await worker.StartAsync(timeout.Token);
        try
        {
            var first = await monitor.Samples.Reader.ReadAsync(timeout.Token);
            var second = await monitor.Samples.Reader.ReadAsync(timeout.Token);
            var third = await monitor.Samples.Reader.ReadAsync(timeout.Token);
            // The original hardcoded 1s delay fails the first assertion.
            Assert.True(Stopwatch.GetElapsedTime(first, second).TotalSeconds >= 1.8);
            var nextGap = Stopwatch.GetElapsedTime(second, third).TotalSeconds;
            Assert.True(nextGap >= (failReload ? 1.8 : 0.8));
            if (!failReload) Assert.True(nextGap < 1.8, $"Expected the new 1s interval, got {nextGap:F2}s.");
            Assert.True(settings.Reads >= 3);
            Assert.Equal("wifi-id", monitor.Selections[0]);
            Assert.Equal(failReload ? "wifi-id" : "ethernet-id", monitor.Selections[1]);
        }
        finally
        {
            // Cancellation must interrupt an outstanding interval wait promptly.
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await worker.StopAsync(stopTimeout.Token);
            Assert.True(worker.ExecuteTask!.IsCompleted);
        }
    }

    private sealed class ChangingSettings(bool failReload) : IApplicationSettingsRepository
    {
        public int Reads { get; private set; }
        public Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default)
        {
            Reads++;
            if (Reads > 1 && failReload)
                throw new InvalidOperationException("Simulated settings read failure");
            return Task.FromResult(new ApplicationSettings
            {
                MonitoringIntervalSeconds = Reads == 1 ? 2 : 1,
                SelectedNetworkAdapter = Reads == 1 ? "wifi-id" : "ethernet-id"
            });
        }
        public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingMonitor : INetworkMonitor
    {
        public List<string?> Selections { get; } = new();
        public Channel<long> Samples { get; } = Channel.CreateUnbounded<long>();
        public NetworkUsage GetUsage(string? selectedNetworkAdapter = null)
        {
            Selections.Add(selectedNetworkAdapter);
            Samples.Writer.TryWrite(Stopwatch.GetTimestamp());
            return new NetworkUsage { Timestamp = DateTime.Now, AdapterName = "Test adapter" };
        }
    }

    private sealed class DiscardUsage : IUsageRepository
    {
        public Task SaveAsync(NetworkUsage usage, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

