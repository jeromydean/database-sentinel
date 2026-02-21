using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BlueFence.DatabaseSentinel.Models;

namespace BlueFence.DatabaseSentinel.Services
{
  public class KeycloakAuthService : IKeycloakAuthService
  {
    private readonly HttpClient _httpClient;
    private readonly string _tokenUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly IAuthService _authService;

    public KeycloakAuthService(
      HttpClient httpClient,
      string authority,
      string realm,
      string clientId,
      IAuthService authService)
    {
      _httpClient = httpClient;
      _realm = realm;
      _clientId = clientId;
      _authService = authService;
      _tokenUrl = $"{authority.TrimEnd('/')}/realms/{realm}/protocol/openid-connect/token";
    }

    public async Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
      FormUrlEncodedContent form = new FormUrlEncodedContent(new Dictionary<string, string>
      {
        ["grant_type"] = "password",
        ["client_id"] = _clientId,
        ["username"] = username,
        ["password"] = password
      });

      HttpResponseMessage response = await _httpClient.PostAsync(_tokenUrl, form, cancellationToken).ConfigureAwait(false);

      if (!response.IsSuccessStatusCode)
      {
        _authService.Clear();
        return false;
      }

      string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
      KeycloakTokenResponse? tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(json);

      if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
      {
        _authService.Clear();
        return false;
      }

      _authService.SetTokens(tokenResponse.AccessToken, tokenResponse.RefreshToken);
      return true;
    }
  }
}
