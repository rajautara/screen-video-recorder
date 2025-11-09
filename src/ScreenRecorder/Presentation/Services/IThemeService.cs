using ScreenRecorder.Domain.Models;

namespace ScreenRecorder.Presentation.Services
{
    public interface IThemeService
    {
        void ApplyTheme(ThemeMode theme);
        ThemeMode CurrentTheme { get; }
    }
}
