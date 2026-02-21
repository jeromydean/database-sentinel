using System;
using System.Threading.Tasks;
using System.Windows.Input;
using BlueFence.DatabaseSentinel.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BlueFence.DatabaseSentinel.ViewModels
{
  public partial class LoginViewModel : ViewModelBase
  {
    private readonly IKeycloakAuthService _keycloakAuth;
    private readonly ILoginSuccessHandler _loginSuccessHandler;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public LoginViewModel(IKeycloakAuthService keycloakAuth, ILoginSuccessHandler loginSuccessHandler)
    {
      _keycloakAuth = keycloakAuth;
      _loginSuccessHandler = loginSuccessHandler;
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
      if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
      {
        ErrorMessage = "Please enter username and password.";
        return;
      }

      ErrorMessage = null;
      IsBusy = true;
      LoginCommand.NotifyCanExecuteChanged();

      try
      {
        bool success = await _keycloakAuth.LoginAsync(Username, Password).ConfigureAwait(true);
        if (success)
        {
          _loginSuccessHandler.OnLoginSucceeded();
        }
        else
        {
          ErrorMessage = "Login failed. Check your username and password.";
        }
      }
      catch (Exception)
      {
        ErrorMessage = "Unable to reach the login server. Is Keycloak running at https://localhost:8443?";
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
  }
}
