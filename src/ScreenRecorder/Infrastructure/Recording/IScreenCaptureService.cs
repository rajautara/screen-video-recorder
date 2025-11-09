using ScreenRecorder.Domain.Models;
using System;

namespace ScreenRecorder.Infrastructure.Recording
{
    public interface IScreenCaptureService
    {
        event EventHandler<RecordingState> StateChanged;
        event EventHandler<string> Error;
        event EventHandler<TimeSpan> ProgressUpdated;

        void StartRecording(RecordingSession session);
        void PauseRecording();
        void ResumeRecording();
        void StopRecording();
        RecordingState CurrentState { get; }
        bool IsHardwareAccelerationAvailable();
        void CheckMediaFoundationAvailability();
    }
}
