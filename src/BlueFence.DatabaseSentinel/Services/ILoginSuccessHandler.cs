using Avalonia.Controls;

namespace BlueFence.DatabaseSentinel.Services
{
  public interface ILoginSuccessHandler
  {
    void SetLoginWindow(Window window);

    void OnLoginSucceeded();
  }
}
