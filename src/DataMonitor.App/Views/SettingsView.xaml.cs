using System.Windows;
using System.Windows.Controls;
using DataMonitor.App.ViewModels;

namespace DataMonitor.App.Views;

public partial class SettingsView : UserControl
{
    private readonly SettingsViewModel _viewModel;
    private bool _loaded;

    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
    }

    private async void Settings_Loaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        await _viewModel.LoadAsync();
    }

    private void RefreshAdapters_Click(object sender, RoutedEventArgs e) => _viewModel.RefreshAdapters();

    private async void Save_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveAsync();
}

