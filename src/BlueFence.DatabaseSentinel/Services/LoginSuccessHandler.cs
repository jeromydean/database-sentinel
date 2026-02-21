using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using BlueFence.DatabaseSentinel.ViewModels;
using BlueFence.DatabaseSentinel.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BlueFence.DatabaseSentinel.Services
{
  public class LoginSuccessHandler : ILoginSuccessHandler
  {
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly IServiceProvider _serviceProvider;
    private Window? _loginWindow;

    public LoginSuccessHandler(
      IClassicDesktopStyleApplicationLifetime desktop,
      IServiceProvider serviceProvider)
    {
      _desktop = desktop;
      _serviceProvider = serviceProvider;
    }

    public void SetLoginWindow(Window window)
    {
      _loginWindow = window;
    }

    public void OnLoginSucceeded()
    {
      if (_loginWindow is null)
      {
        return;
      }

      MainWindow mainWindow = new MainWindow
      {
        DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
      };
      mainWindow.Show();
      _desktop.MainWindow = mainWindow;
      _loginWindow.Close();
    }
  }
}
