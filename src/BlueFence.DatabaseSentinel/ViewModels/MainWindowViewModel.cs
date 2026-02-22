using System;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueFence.DatabaseSentinel.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace BlueFence.DatabaseSentinel.ViewModels
{
  public partial class MainWindowViewModel : ViewModelBase
  {
    private readonly IAuthService _authService;
    private readonly IKeycloakAuthService _keycloakAuth;
    private readonly ILoginSuccessHandler _loginSuccessHandler;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public MainWindowViewModel(
      IAuthService authService,
      IKeycloakAuthService keycloakAuth,
      ILoginSuccessHandler loginSuccessHandler)
    {
      _authService = authService;
      _keycloakAuth = keycloakAuth;
      _loginSuccessHandler = loginSuccessHandler;
      _loginSuccessHandler.LoginSucceeded += OnLoginSucceeded;
      _ = EnsureAuthenticatedAsync();
    }

    /// <summary>Runs on startup: if not authenticated, triggers MSAL login (no app UI shown until success or retry).</summary>
    private async Task EnsureAuthenticatedAsync()
    {
      if (_keycloakAuth is null || _loginSuccessHandler is null) return;
      if (_authService.IsAuthenticated)
      {
        _loginSuccessHandler.ShowMainWindow();
        return;
      }
      await LoginAsync().ConfigureAwait(true);
    }

    /// <summary>Parameterless constructor for design-time XAML only.</summary>
    public MainWindowViewModel()
    {
      _authService = new AuthService();
      _keycloakAuth = null!;
      _loginSuccessHandler = null!;
    }

    public bool IsLoggedIn => _authService.IsAuthenticated;

    public bool IsNotLoggedIn => !_authService.IsAuthenticated;

    private void OnLoginSucceeded()
    {
      OnPropertyChanged(nameof(IsLoggedIn));
      OnPropertyChanged(nameof(IsNotLoggedIn));
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
      if (_keycloakAuth is null || _loginSuccessHandler is null)
      {
        return;
      }

      ErrorMessage = null;
      IsBusy = true;
      LoginCommand.NotifyCanExecuteChanged();

      try
      {
        bool success = await _keycloakAuth.LoginAsync().ConfigureAwait(true);
        if (success)
        {
          _loginSuccessHandler.OnLoginSucceeded();
        }
        else
        {
          ErrorMessage = "Login was cancelled or failed.";
          _loginSuccessHandler.ShowMainWindow();
        }
      }
      catch (Exception ex)
      {
        ErrorMessage = ex.Message ?? "Unable to reach the login server. Is Keycloak running at https://localhost:8443?";
        _loginSuccessHandler?.ShowMainWindow();
      }
      finally
      {
        IsBusy = false;
        LoginCommand.NotifyCanExecuteChanged();
      }
    }

    private bool CanLogin() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
      LoginCommand.NotifyCanExecuteChanged();
      OnPropertyChanged(nameof(IsNotBusy));
    }

    public bool IsNotBusy => !IsBusy;

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
