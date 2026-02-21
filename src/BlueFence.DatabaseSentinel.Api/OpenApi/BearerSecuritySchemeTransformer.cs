using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BlueFence.DatabaseSentinel.Api.OpenApi
{
  /// <summary>
  /// Adds JWT Bearer security scheme and requirement to the OpenAPI document when Bearer authentication is registered.
  /// Enables the "Authorize" button in Swagger UI so you can paste a token for testing.
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
        Description = "Paste a Keycloak access token. Use POST /dev/token (Development only) to get one."
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
