using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BlueFence.DatabaseSentinel.Services;
using BlueFence.DatabaseSentinel.ViewModels;
using BlueFence.DatabaseSentinel.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace BlueFence.DatabaseSentinel
{
  public partial class App : Application
  {
    public override void Initialize()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
      if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
        DisableAvaloniaDataAnnotationValidation();
        desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

        IServiceProvider services = ConfigureServices(desktop);

        ILoginSuccessHandler loginSuccessHandler = services.GetRequiredService<ILoginSuccessHandler>();
        MainWindow mainWindow = new MainWindow
        {
          DataContext = services.GetRequiredService<MainWindowViewModel>()
        };
        loginSuccessHandler.SetMainWindow(mainWindow);
      }

      base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider ConfigureServices(IClassicDesktopStyleApplicationLifetime desktop)
    {
      ServiceCollection collection = new ServiceCollection();

      collection.AddSingleton(desktop);
      collection.AddSingleton<IAuthService, AuthService>();
      collection.AddSingleton<IMsalHttpClientFactory, MsalHttpClientFactory>();
      collection.AddSingleton<IKeycloakAuthService, MsalKeycloakAuthService>();
      collection.AddSingleton<ILoginSuccessHandler, LoginSuccessHandler>();
      collection.AddTransient<MainWindowViewModel>();

      return collection.BuildServiceProvider();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
      //// Get an array of plugins to remove
      //DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove =
      //  BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

      //// remove each entry found
      //foreach (DataAnnotationsValidationPlugin plugin in dataValidationPluginsToRemove)
      //{
      //  BindingPlugins.DataValidators.Remove(plugin);
      //}
    }
  }
}