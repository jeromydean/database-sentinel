namespace BlueFence.DatabaseSentinel.Services
{
  public class AuthService : IAuthService
  {
    private string? _accessToken;
    private string? _refreshToken;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    public string? AccessToken => _accessToken;

    public void SetTokens(string accessToken, string? refreshToken)
    {
      _accessToken = accessToken;
      _refreshToken = refreshToken;
    }

    public void Clear()
    {
      _accessToken = null;
      _refreshToken = null;
    }
  }
}
