using BlueFence.DatabaseSentinel.Api.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BlueFence.DatabaseSentinel.Api
{
  public class Program
  {
    public static void Main(string[] args)
    {
      WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

      builder.Services.AddControllers();
      builder.Services.AddOpenApi(options =>
      {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
      });

      IConfigurationSection keycloakSection = builder.Configuration.GetSection("Keycloak");
      string authority = keycloakSection["Authority"] ?? "https://localhost:8443/realms/database-sentinel";
      string? audience = keycloakSection["Audience"];

      builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
          options.Authority = authority;
          options.Audience = audience;
          options.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateAudience = !string.IsNullOrEmpty(audience)
          };
          if (builder.Environment.IsDevelopment())
          {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
              ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
          }
        });
      builder.Services.AddAuthorization();

      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddHttpClient("KeycloakDev").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
      {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
      });

      WebApplication app = builder.Build();

      if (app.Environment.IsDevelopment())
      {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
          options.SwaggerEndpoint("/openapi/v1.json", "Database Sentinel API v1");
        });
      }

      app.UseHttpsRedirection();
      app.UseAuthentication();
      app.UseAuthorization();
      app.MapControllers();

      app.Run();
    }
  }
}