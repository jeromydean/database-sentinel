using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
#if USE_MSAL_EMBEDDED_WEBVIEW
using Microsoft.Identity.Client.Desktop;
#endif

namespace BlueFence.DatabaseSentinel.Services
{
  /// <summary>
  /// Keycloak login via MSAL using Authorization Code + PKCE.
  /// On Windows, uses embedded WebView2 in its own window when available.
  /// </summary>
  public class MsalKeycloakAuthService : IKeycloakAuthService
  {
    private readonly IAuthService _authService;
    private readonly IPublicClientApplication _pca;

    public MsalKeycloakAuthService(
      IAuthService authService,
      IMsalHttpClientFactory httpClientFactory)
    {
      _authService = authService;
      PublicClientApplicationBuilder builder = PublicClientApplicationBuilder
        .Create("database-sentinel-ui")
        .WithExperimentalFeatures()
        .WithOidcAuthority("https://localhost:8443/realms/database-sentinel")
        .WithRedirectUri("http://localhost:46421")
        .WithHttpClientFactory(httpClientFactory);
#if USE_MSAL_EMBEDDED_WEBVIEW
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        builder = builder.WithWindowsEmbeddedBrowserSupport();
      }
#endif
      _pca = builder.Build();
    }

    public async Task<bool> LoginAsync(CancellationToken cancellationToken = default)
    {
      try
      {
        System.Collections.Generic.IEnumerable<string> scopes = new[] { "openid", "profile" };
        AcquireTokenInteractiveParameterBuilder interactive = _pca.AcquireTokenInteractive(scopes);
#if USE_MSAL_EMBEDDED_WEBVIEW
        // Do not pass parent handle: MSAL shows its own top-level window for the embedded
        // WebView2. WithParentActivityOrWindow(Avalonia handle) can result in a child window
        // that is hidden or zero-sized, so the user sees no login UI.
        // If (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { ... WithParentActivityOrWindow(...) }
#endif
        AuthenticationResult result = await interactive
          .ExecuteAsync(cancellationToken)
          .ConfigureAwait(true);

        if (result != null && !string.IsNullOrEmpty(result.AccessToken))
        {
          _authService.SetTokens(result.AccessToken, null);
          return true;
        }

        _authService.Clear();
        return false;
      }
      catch (MsalException)
      {
        _authService.Clear();
        return false;
      }
    }

  }
}
