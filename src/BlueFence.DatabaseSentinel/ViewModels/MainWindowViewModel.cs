using BlueFence.DatabaseSentinel.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace BlueFence.DatabaseSentinel.ViewModels
{
  public partial class MainWindowViewModel : ViewModelBase
  {
    private readonly IAuthService _authService;

    public MainWindowViewModel(IAuthService authService)
    {
      _authService = authService;
    }

    public string AppTitle { get; } = "Database Sentinel";

    public string PageTitle { get; } = "Dashboard";

    public int MonitoredServerCount { get; } = 0;

    public int ActiveAlertCount { get; } = 0;

    public string ServerStatusSummary { get; } = "No servers configured. Add a server to begin monitoring.";

    public string AlertSummary { get; } = "No active alerts.";

    public ISeries[] ActivitySeries { get; } =
    [
      new LineSeries<double>
      {
        Name = "CPU %",
        Values = new double[] { 12, 15, 18, 14, 20, 22, 19 }
      },
      new LineSeries<double>
      {
        Name = "Waits",
        Values = new double[] { 8, 11, 9, 14, 12, 10, 13 }
      },
      new LineSeries<double>
      {
        Name = "Memory (GB)",
        Values = new double[] { 6, 10, 12, 14, 20, 36, 48 }
      }
    ];
  }
}
