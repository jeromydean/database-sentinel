using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace BlueFence.DatabaseSentinel.Services
{
  public class LoginSuccessHandler : ILoginSuccessHandler
  {
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private Window? _mainWindow;

    public LoginSuccessHandler(IClassicDesktopStyleApplicationLifetime desktop)
    {
      _desktop = desktop;
    }

    public event Action? LoginSucceeded;

    public void SetMainWindow(Window window)
    {
      _mainWindow = window;
    }

    public void OnLoginSucceeded()
    {
      LoginSucceeded?.Invoke();
      ShowMainWindow();
    }

    public void ShowMainWindow()
    {
      if (_mainWindow is null) return;
      _desktop.MainWindow = _mainWindow;
      _desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
      _mainWindow.Show();
      _mainWindow.Activate();
    }
  }
}
