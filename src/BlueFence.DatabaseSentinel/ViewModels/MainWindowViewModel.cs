using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace BlueFence.DatabaseSentinel.ViewModels
{
  public partial class MainWindowViewModel : ViewModelBase
  {
    public string Greeting { get; } = "Welcome to Avalonia!";

    public ISeries[] DashboardSeries { get; } =
    [
      new LineSeries<double>
      {
        Name = "Sample metric",
        Values = new double[] { 12, 15, 18, 14, 20, 22, 19 }
      },
      new ColumnSeries<double>
      {
        Name = "Sample count",
        Values = new double[] { 8, 11, 9, 14, 12, 10, 13 }
      }
    ];
  }
}
