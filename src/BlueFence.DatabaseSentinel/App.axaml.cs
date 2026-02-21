using System;
using System.Linq;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BlueFence.DatabaseSentinel.Services;
using BlueFence.DatabaseSentinel.ViewModels;
using BlueFence.DatabaseSentinel.Views;
using Microsoft.Extensions.DependencyInjection;

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
        // Only shut down when the current main window closes (so replacing login with main doesn't exit).
        desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;

        IServiceProvider services = ConfigureServices(desktop);

        ILoginSuccessHandler loginSuccessHandler = services.GetRequiredService<ILoginSuccessHandler>();
        LoginWindow loginWindow = new LoginWindow();
        loginSuccessHandler.SetLoginWindow(loginWindow);
        loginWindow.DataContext = services.GetRequiredService<LoginViewModel>();

        desktop.MainWindow = loginWindow;
      }

      base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider ConfigureServices(IClassicDesktopStyleApplicationLifetime desktop)
    {
      ServiceCollection collection = new ServiceCollection();

      collection.AddSingleton(desktop);
      collection.AddSingleton(CreateKeycloakHttpClient());
      collection.AddSingleton<IAuthService, AuthService>();
      collection.AddSingleton<IKeycloakAuthService>(sp => new KeycloakAuthService(
        sp.GetRequiredService<HttpClient>(),
        authority: "https://localhost:8443",
        realm: "database-sentinel",
        clientId: "database-sentinel-ui",
        sp.GetRequiredService<IAuthService>()));
      collection.AddSingleton<ILoginSuccessHandler, LoginSuccessHandler>();
      collection.AddTransient<MainWindowViewModel>();
      collection.AddTransient<LoginViewModel>();

      return collection.BuildServiceProvider();
    }

    private static HttpClient CreateKeycloakHttpClient()
    {
      HttpClientHandler handler = new HttpClientHandler();
      handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
      return new HttpClient(handler);
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