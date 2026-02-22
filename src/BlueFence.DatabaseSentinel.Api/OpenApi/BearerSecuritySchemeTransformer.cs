using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BlueFence.DatabaseSentinel.Api.OpenApi
{
  /// <summary>
  /// Adds a JWT Bearer (paste-token) security scheme to the OpenAPI document when Bearer authentication is registered.
  /// Not used by default: Swagger uses OAuth2 (Authorization Code + PKCE) via <see cref="OAuth2SecuritySchemeTransformer"/>.
  /// Register this transformer in AddOpenApi only if you also want to allow manually pasting a token in Swagger.
  /// </summary>
  internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
  {
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
      IEnumerable<AuthenticationScheme> schemes = await authenticationSchemeProvider.GetAllSchemesAsync().ConfigureAwait(false);
      if (!schemes.Any(s => s.Name == "Bearer"))
      {
        return;
      }

      document.Components ??= new OpenApiComponents();
      document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
      document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
      {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        In = ParameterLocation.Header,
        BearerFormat = "JWT",
        Description = "Optional: paste a Keycloak access token here. By default use Authorize to sign in with OAuth2 + PKCE."
      };

      OpenApiSecuritySchemeReference bearerRef = new OpenApiSecuritySchemeReference("Bearer", document);
      if (document.Paths is null)
      {
        return;
      }
      foreach (IOpenApiPathItem pathItem in document.Paths.Values)
      {
        if (pathItem.Operations is null)
        {
          continue;
        }
        foreach (KeyValuePair<HttpMethod, OpenApiOperation> op in pathItem.Operations)
        {
          op.Value.Security ??= [];
          op.Value.Security.Add(new OpenApiSecurityRequirement { [bearerRef] = [] });
        }
      }
    }
  }
}
