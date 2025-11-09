using ScreenRecorder.Domain.Models;
using System;

namespace ScreenRecorder.Domain.Orchestration
{
    public interface IRecordingController
    {
        RecordingState CurrentState { get; }
        RecordingSession CurrentSession { get; }
        RecordingProfile ActiveProfile { get; set; }

        event EventHandler<RecordingState> StateChanged;
        event EventHandler<RecordingSession> RecordingStarted;
        event EventHandler<RecordingSession> RecordingPaused;
        event EventHandler<RecordingSession> RecordingResumed;
        event EventHandler<RecordingSession> RecordingCompleted;
        event EventHandler<string> Error;
        event EventHandler<TimeSpan> ProgressUpdated;

        void StartRecording();
        void PauseRecording();
        void ResumeRecording();
        void StopRecording();
        void CancelRecording();
        void DropMarker();
    }
}
