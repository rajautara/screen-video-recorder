using System;

namespace ScreenRecorder.Presentation.Services
{
    public interface ITrayService : IDisposable
    {
        void Initialize();
        void ShowBalloonTip(string title, string text);
        void UpdateIcon(string iconPath);
        void UpdateTooltip(string tooltip);
        event EventHandler TrayIconClicked;
        event EventHandler ExitRequested;
    }
}
