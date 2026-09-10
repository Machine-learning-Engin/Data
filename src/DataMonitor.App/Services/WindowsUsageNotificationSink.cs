using DataMonitor.Core.Interfaces;
using DataMonitor.Core.Models;
using System.Windows;
using Forms = System.Windows.Forms;

namespace DataMonitor.App.Services;

public sealed class WindowsUsageNotificationSink : IUsageNotificationSink, IDisposable
{
    private Forms.NotifyIcon? _icon;
    public async Task ShowAsync(UsageAlertStatus status, decimal percentage, CancellationToken cancellationToken = default)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _icon ??= new Forms.NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Information,
                Text = "DataMonitor", Visible = true
            };
            var prefix = status == UsageAlertStatus.Critical ? "Critical: " : "";
            _icon.ShowBalloonTip(10000, "DataMonitor", $"{prefix}You have used {percentage:F1}% of your monthly data allowance.",
                status == UsageAlertStatus.Critical ? Forms.ToolTipIcon.Error : Forms.ToolTipIcon.Warning);
        });
    }
    public void Dispose() { _icon?.Dispose(); }
}
