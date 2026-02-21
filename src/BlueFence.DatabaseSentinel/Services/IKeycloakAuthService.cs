using System.Threading;
using System.Threading.Tasks;

namespace BlueFence.DatabaseSentinel.Services
{
  public interface IKeycloakAuthService
  {
    Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
  }
}
