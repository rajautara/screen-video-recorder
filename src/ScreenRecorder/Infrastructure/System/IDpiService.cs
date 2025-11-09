using System.Windows;

namespace ScreenRecorder.Infrastructure.System
{
    public interface IDpiService
    {
        double GetDpiScale();
        double GetDpiScaleForWindow(Window window);
        void SetDpiAwareness();
    }
}
