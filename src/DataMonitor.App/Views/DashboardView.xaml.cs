using DataMonitor.App.Resources;
using DataMonitor.App.ViewModels;

namespace DataMonitor.App.Views;

public partial class DashboardView
{
    public DashboardView(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) =>
        {
            ChartTheme.Apply(TrafficChart);
            await viewModel.StartAsync();
        };
        Unloaded += (_, _) => viewModel.Stop();
    }
}

