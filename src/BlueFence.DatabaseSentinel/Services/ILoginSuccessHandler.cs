using Avalonia.Controls;

namespace BlueFence.DatabaseSentinel.Services
{
  public interface ILoginSuccessHandler
  {
    event Action? LoginSucceeded;

    void SetMainWindow(Window window);

    void OnLoginSucceeded();

    void ShowMainWindow();
  }
}
