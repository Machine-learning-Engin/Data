using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DataMonitor.App.Views;

public partial class MainWindow
{
    private readonly IServiceProvider _services;

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        ShowDashboard();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var darkTitleBar = 1;
        DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref darkTitleBar, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    private void Dashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();

    private void History_Click(object sender, RoutedEventArgs e)
    {
        HistoryNav.IsChecked = true;
        DashboardContainer.Content = _services.GetRequiredService<HistoryView>();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        SettingsNav.IsChecked = true;
        DashboardContainer.Content = _services.GetRequiredService<SettingsView>();
    }

    private void ShowDashboard()
    {
        DashboardNav.IsChecked = true;
        var dashboard = _services.GetRequiredService<DashboardView>();
        MonitoringPanel.DataContext = dashboard.DataContext;
        DashboardContainer.Content = dashboard;
    }
}
