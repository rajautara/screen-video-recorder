using System;

namespace ScreenRecorder.Infrastructure.System
{
    public interface IPowerEventsService
    {
        event EventHandler SystemSuspending;
        event EventHandler SystemResuming;
        event EventHandler PowerModeChanged;
        void StartMonitoring();
        void StopMonitoring();
    }
}
