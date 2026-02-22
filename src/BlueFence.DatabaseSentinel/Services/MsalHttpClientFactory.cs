using System.Net.Http;
using Microsoft.Identity.Client;

namespace BlueFence.DatabaseSentinel.Services
{
  /// <summary>
  /// Provides an HttpClient that skips TLS validation for Keycloak (dev self-signed cert).
  /// Reuses a single instance to avoid socket exhaustion.
  /// </summary>
  public sealed class MsalHttpClientFactory : IMsalHttpClientFactory
  {
    private static readonly HttpClient SharedClient = CreateInsecureClient();

    public HttpClient GetHttpClient()
    {
      return SharedClient;
    }

    private static HttpClient CreateInsecureClient()
    {
      HttpClientHandler handler = new HttpClientHandler();
      handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
      return new HttpClient(handler);
    }
  }
}
