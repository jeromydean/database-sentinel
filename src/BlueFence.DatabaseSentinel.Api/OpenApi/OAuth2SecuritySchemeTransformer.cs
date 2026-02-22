using System.Net.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BlueFence.DatabaseSentinel.Api.OpenApi
{
  /// <summary>
  /// Adds OAuth2 (Authorization Code + PKCE) security scheme so Swagger UI can authenticate with Keycloak like the desktop app.
  /// </summary>
  internal sealed class OAuth2SecuritySchemeTransformer : IOpenApiDocumentTransformer
  {
    private readonly IConfiguration _configuration;

    public OAuth2SecuritySchemeTransformer(IConfiguration configuration)
    {
      _configuration = configuration;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
      string authority = _configuration["Keycloak:Authority"]?.TrimEnd('/')
        ?? "https://localhost:8443/realms/database-sentinel";
      string authorizationUrl = $"{authority}/protocol/openid-connect/auth";
      string tokenUrl = $"{authority}/protocol/openid-connect/token";

      document.Components ??= new OpenApiComponents();
      document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
      document.Components.SecuritySchemes["oauth2"] = new OpenApiSecurityScheme
      {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
          AuthorizationCode = new OpenApiOAuthFlow
          {
            AuthorizationUrl = new Uri(authorizationUrl),
            TokenUrl = new Uri(tokenUrl),
            Scopes = new Dictionary<string, string>
            {
              ["openid"] = "OpenID Connect",
              ["profile"] = "Profile"
            }
          }
        },
        Description = "Sign in with Keycloak (Authorization Code + PKCE). Same flow as the desktop app."
      };

      OpenApiSecuritySchemeReference oauth2Ref = new OpenApiSecuritySchemeReference("oauth2", document);
      if (document.Paths is null)
      {
        return Task.CompletedTask;
      }
      foreach (IOpenApiPathItem pathItem in document.Paths.Values)
      {
        if (pathItem.Operations is null) continue;
        foreach (KeyValuePair<HttpMethod, OpenApiOperation> op in pathItem.Operations)
        {
          op.Value.Security ??= [];
          op.Value.Security.Add(new OpenApiSecurityRequirement { [oauth2Ref] = [] });
        }
      }
      return Task.CompletedTask;
    }
  }
}
