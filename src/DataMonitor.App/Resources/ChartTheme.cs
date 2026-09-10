using System.Windows;
using System.Windows.Media;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;

namespace DataMonitor.App.Resources;

public static class ChartTheme
{
    public static SKColor Color(string key)
    {
        var color = ((SolidColorBrush)Application.Current.FindResource(key)).Color;
        return new SKColor(color.R, color.G, color.B, color.A);
    }

    public static void Apply(CartesianChart chart)
    {
        chart.LegendTextPaint = new SolidColorPaint(Color("SecondaryTextBrush"));
        chart.TooltipTextPaint = new SolidColorPaint(Color("TextBrush"));
        chart.TooltipBackgroundPaint = new SolidColorPaint(Color("CardBrush"));
        foreach (var axis in chart.XAxes.Concat(chart.YAxes).OfType<Axis>())
        {
            axis.SeparatorsPaint = new SolidColorPaint(Color("LineBrush")) { StrokeThickness = 1 };
        }
    }
}
