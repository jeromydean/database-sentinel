using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueFence.DatabaseSentinel.Api.Controllers
{
  /// <summary>
  /// Development-only endpoint to obtain a Keycloak token for testing protected APIs in Swagger.
  /// Not available in production.
  /// </summary>
  [ApiController]
  [Route("dev")]
  [AllowAnonymous]
  public class DevTokenController : ControllerBase
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public DevTokenController(
      IHttpClientFactory httpClientFactory,
      IConfiguration configuration,
      IWebHostEnvironment environment)
    {
      _httpClientFactory = httpClientFactory;
      _configuration = configuration;
      _environment = environment;
    }

    /// <summary>
    /// Exchange username and password for a Keycloak access token (resource owner password grant).
    /// Use the returned access_token in Swagger: click Authorize and paste "Bearer {access_token}".
    /// Only available in Development.
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(DevTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetToken([FromBody] DevTokenRequest request, CancellationToken cancellationToken)
    {
      if (!_environment.IsDevelopment())
      {
        return NotFound();
      }

      if (string.IsNullOrWhiteSpace(request?.Username) || string.IsNullOrWhiteSpace(request?.Password))
      {
        return BadRequest(new { error = "Username and Password are required." });
      }

      string authority = _configuration["Keycloak:Authority"] ?? "https://localhost:8443/realms/database-sentinel";
      string clientId = _configuration["Keycloak:DevTokenClientId"] ?? "database-sentinel-ui";
      string tokenUrl = $"{authority.TrimEnd('/')}/protocol/openid-connect/token";

      HttpClient client = _httpClientFactory.CreateClient("KeycloakDev");
      Dictionary<string, string> form = new Dictionary<string, string>
      {
        ["grant_type"] = "password",
        ["client_id"] = clientId,
        ["username"] = request.Username,
        ["password"] = request.Password
      };
      using (FormUrlEncodedContent content = new FormUrlEncodedContent(form))
      {
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        HttpResponseMessage response = await client.PostAsync(tokenUrl, content, cancellationToken).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
          return BadRequest(new { error = "Keycloak returned an error.", details = json });
        }

        using (JsonDocument doc = JsonDocument.Parse(json))
        {
          string? accessToken = doc.RootElement.TryGetProperty("access_token", out JsonElement accessEl)
            ? accessEl.GetString()
            : null;
          if (string.IsNullOrEmpty(accessToken))
          {
            return BadRequest(new { error = "No access_token in Keycloak response.", details = json });
          }

          return Ok(new DevTokenResponse { AccessToken = accessToken });
        }
      }
    }
  }

  public class DevTokenRequest
  {
    public string? Username { get; set; }
    public string? Password { get; set; }
  }

  public class DevTokenResponse
  {
    public string AccessToken { get; set; } = string.Empty;
  }
}
