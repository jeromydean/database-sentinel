namespace BlueFence.DatabaseSentinel.Services
{
  public interface IAuthService
  {
    bool IsAuthenticated { get; }

    string? AccessToken { get; }

    void SetTokens(string accessToken, string? refreshToken);

    void Clear();
  }
}
