using DataMonitor.App.Resources;
using DataMonitor.App.ViewModels;

using System.Windows;
using System.Windows.Controls;


namespace DataMonitor.App.Views;


public partial class HistoryView : UserControl
{

    private readonly HistoryViewModel
        _viewModel;


    private bool
        _initialLoadCompleted;



    public HistoryView(
        HistoryViewModel viewModel)
    {

        InitializeComponent();


        _viewModel =
            viewModel;


        DataContext =
            _viewModel;


        Loaded +=
            HistoryView_Loaded;

    }





    private async void HistoryView_Loaded(
        object sender,
        RoutedEventArgs e)
    {

        if (_initialLoadCompleted)
        {
            return;
        }


        ChartTheme.Apply(HistoryChart);

        _initialLoadCompleted =
            true;


        await _viewModel
            .LoadTodayAsync();

    }





    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export selected History period",
            Filter = "CSV (*.csv)|*.csv|JSON (*.json)|*.json",
            FileName = $"DataMonitor_History_{DateTime.Today:yyyy-MM-dd}",
            AddExtension = true, OverwritePrompt = true
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        string? temporary = null;
        try
        {
            var content = await _viewModel.CreateExportAsync(dialog.FilterIndex == 2);
            temporary = dialog.FileName + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await System.IO.File.WriteAllTextAsync(temporary, content, new System.Text.UTF8Encoding(false));
            System.IO.File.Move(temporary, dialog.FileName, true);
            _viewModel.ReportExport("History exported successfully.");
        }
        catch (Exception ex)
        {
            Infrastructure.Diagnostics.ReleaseLog.Write("History export failed", ex);
            _viewModel.ReportExport("Could not export History. Check the destination and try again.");
        }
        finally
        {
            if (temporary is not null)
                try { System.IO.File.Delete(temporary); } catch { }
        }
    }

    private async void Today_Click(
        object sender,
        RoutedEventArgs e)
    {

        await _viewModel
            .LoadTodayAsync();

    }





    private async void Week_Click(
        object sender,
        RoutedEventArgs e)
    {

        await _viewModel
            .LoadWeekAsync();

    }





    private async void Month_Click(
        object sender,
        RoutedEventArgs e)
    {

        await _viewModel
            .LoadMonthAsync();

    }

}

