using System.Threading;
using System.Threading.Tasks;

namespace BlueFence.DatabaseSentinel.Services
{
  public interface IKeycloakAuthService
  {
    /// <summary>
    /// Starts the interactive Authorization Code + PKCE flow (opens system browser on Windows).
    /// </summary>
    Task<bool> LoginAsync(CancellationToken cancellationToken = default);
  }
}
