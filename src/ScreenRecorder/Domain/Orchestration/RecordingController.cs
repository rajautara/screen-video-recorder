using System;
using System.Threading.Tasks;
using System.Timers;
using ScreenRecorder.Domain.Models;
using ScreenRecorder.Infrastructure.Recording;
using ScreenRecorder.Infrastructure.Storage;
using ScreenRecorder.Infrastructure.Settings;
using ScreenRecorder.Infrastructure.System;
using Serilog;

namespace ScreenRecorder.Domain.Orchestration
{
    public class RecordingController : IRecordingController, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IScreenCaptureService _captureService;
        private readonly IStorageService _storageService;
        private readonly ISettingsService _settingsService;
        private readonly IPowerEventsService _powerEventsService;

        private RecordingSession _currentSession;
        private RecordingProfile _activeProfile;
        private Timer _autoStopTimer;
        private Timer _autoSplitTimer;
        private bool _disposed;

        public RecordingState CurrentState => _captureService.CurrentState;
        public RecordingSession CurrentSession => _currentSession;

        public RecordingProfile ActiveProfile
        {
            get => _activeProfile;
            set
            {
                _activeProfile = value;
                _logger.Information("Active profile changed to: {ProfileName}", value?.Name);
            }
        }

        public event EventHandler<RecordingState> StateChanged;
        public event EventHandler<RecordingSession> RecordingStarted;
        public event EventHandler<RecordingSession> RecordingPaused;
        public event EventHandler<RecordingSession> RecordingResumed;
        public event EventHandler<RecordingSession> RecordingCompleted;
        public event EventHandler<string> Error;
        public event EventHandler<TimeSpan> ProgressUpdated;

        public RecordingController(
            ILogger logger,
            IScreenCaptureService captureService,
            IStorageService storageService,
            ISettingsService settingsService,
            IPowerEventsService powerEventsService)
        {
            _logger = logger;
            _captureService = captureService;
            _storageService = storageService;
            _settingsService = settingsService;
            _powerEventsService = powerEventsService;

            // Wire up events
            _captureService.StateChanged += OnCaptureStateChanged;
            _captureService.Error += OnCaptureError;
            _captureService.ProgressUpdated += OnProgressUpdated;

            _powerEventsService.SystemSuspending += OnSystemSuspending;
            _powerEventsService.StartMonitoring();

            // Load active profile
            LoadActiveProfile();

            _logger.Information("RecordingController initialized");
        }

        public void StartRecording()
        {
            try
            {
                if (CurrentState != RecordingState.Idle)
                {
                    throw new InvalidOperationException($"Cannot start recording in {CurrentState} state");
                }

                if (_activeProfile == null)
                {
                    throw new InvalidOperationException("No active profile selected");
                }

                _logger.Information("Starting recording with profile: {ProfileName}", _activeProfile.Name);

                // Create new session
                _currentSession = new RecordingSession
                {
                    Profile = _activeProfile.Clone(),
                    StartTime = DateTime.Now
                };

                // Get temp file path
                _currentSession.TempFilePath = _storageService.GetTempFilePath(_currentSession);

                // Save recovery info
                _storageService.SaveRecoveryInfo(_currentSession);

                // Set up auto-stop timer if configured
                if (_activeProfile.AutoStopSeconds > 0)
                {
                    SetupAutoStopTimer(_activeProfile.AutoStopSeconds);
                }

                // Set up auto-split timer if configured
                if (_activeProfile.AutoSplitMinutes > 0)
                {
                    SetupAutoSplitTimer(_activeProfile.AutoSplitMinutes);
                }

                // Start capture
                _captureService.StartRecording(_currentSession);

                RecordingStarted?.Invoke(this, _currentSession);
                _logger.Information("Recording started: Session {SessionId}", _currentSession.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start recording");
                Error?.Invoke(this, ex.Message);
                CleanupSession();
                throw;
            }
        }

        public void PauseRecording()
        {
            try
            {
                if (CurrentState != RecordingState.Recording)
                {
                    throw new InvalidOperationException("Cannot pause when not recording");
                }

                _captureService.PauseRecording();
                StopTimers();

                RecordingPaused?.Invoke(this, _currentSession);
                _logger.Information("Recording paused");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to pause recording");
                Error?.Invoke(this, ex.Message);
            }
        }

        public void ResumeRecording()
        {
            try
            {
                if (CurrentState != RecordingState.Paused)
                {
                    throw new InvalidOperationException("Cannot resume when not paused");
                }

                _captureService.ResumeRecording();
                RestartTimers();

                RecordingResumed?.Invoke(this, _currentSession);
                _logger.Information("Recording resumed");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to resume recording");
                Error?.Invoke(this, ex.Message);
            }
        }

        public void StopRecording()
        {
            try
            {
                if (CurrentState == RecordingState.Idle)
                {
                    return;
                }

                _logger.Information("Stopping recording...");
                StopTimers();

                _captureService.StopRecording();

                // Finalize in background
                Task.Run(() => FinalizeRecording());
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to stop recording");
                Error?.Invoke(this, ex.Message);
            }
        }

        public void CancelRecording()
        {
            try
            {
                _logger.Information("Cancelling recording...");
                StopTimers();

                _captureService.StopRecording();

                // Delete temp file
                if (_currentSession != null && !string.IsNullOrEmpty(_currentSession.TempFilePath))
                {
                    _storageService.DeleteFile(_currentSession.TempFilePath);
                    _storageService.ClearRecoverySession(_currentSession.Id);
                }

                CleanupSession();
                _logger.Information("Recording cancelled");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to cancel recording");
                Error?.Invoke(this, ex.Message);
            }
        }

        public void DropMarker()
        {
            if (_currentSession != null && CurrentState == RecordingState.Recording)
            {
                var markerTime = _currentSession.GetEffectiveDuration();
                _currentSession.AddMarker(markerTime);
                _logger.Information("Marker dropped at {Time}", markerTime);
            }
        }

        private void FinalizeRecording()
        {
            try
            {
                if (_currentSession == null)
                    return;

                _logger.Information("Finalizing recording...");

                if (_storageService.FinalizeRecording(_currentSession))
                {
                    _logger.Information("Recording finalized: {Path}", _currentSession.FinalFilePath);
                    RecordingCompleted?.Invoke(this, _currentSession);
                }
                else
                {
                    _logger.Error("Failed to finalize recording");
                    Error?.Invoke(this, "Failed to finalize recording");
                }

                CleanupSession();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during recording finalization");
                Error?.Invoke(this, ex.Message);
            }
        }

        private void SetupAutoStopTimer(int seconds)
        {
            _autoStopTimer = new Timer(seconds * 1000);
            _autoStopTimer.Elapsed += (s, e) =>
            {
                _logger.Information("Auto-stop timer triggered");
                StopRecording();
            };
            _autoStopTimer.AutoReset = false;
            _autoStopTimer.Start();
        }

        private void SetupAutoSplitTimer(int minutes)
        {
            _autoSplitTimer = new Timer(minutes * 60 * 1000);
            _autoSplitTimer.Elapsed += (s, e) =>
            {
                _logger.Information("Auto-split timer triggered");
                // Implement split logic here
                SplitRecording();
            };
            _autoSplitTimer.AutoReset = true;
            _autoSplitTimer.Start();
        }

        private void SplitRecording()
        {
            // Save current recording
            StopRecording();

            // Start new segment
            Task.Delay(1000).ContinueWith(_ =>
            {
                _currentSession.CurrentSegment++;
                StartRecording();
            });
        }

        private void StopTimers()
        {
            _autoStopTimer?.Stop();
            _autoSplitTimer?.Stop();
        }

        private void RestartTimers()
        {
            _autoStopTimer?.Start();
            _autoSplitTimer?.Start();
        }

        private void CleanupSession()
        {
            _autoStopTimer?.Dispose();
            _autoStopTimer = null;

            _autoSplitTimer?.Dispose();
            _autoSplitTimer = null;

            _currentSession = null;
        }

        private void LoadActiveProfile()
        {
            var settings = _settingsService.Current;
            if (!string.IsNullOrEmpty(settings.ActiveProfileId))
            {
                _activeProfile = settings.Profiles.Find(p => p.Id == settings.ActiveProfileId);
            }

            if (_activeProfile == null && settings.Profiles.Count > 0)
            {
                _activeProfile = settings.Profiles[0];
            }
        }

        private void OnCaptureStateChanged(object sender, RecordingState state)
        {
            StateChanged?.Invoke(this, state);
        }

        private void OnCaptureError(object sender, string error)
        {
            _logger.Error("Capture error: {Error}", error);
            Error?.Invoke(this, error);
        }

        private void OnProgressUpdated(object sender, TimeSpan elapsed)
        {
            if (_currentSession != null)
            {
                ProgressUpdated?.Invoke(this, elapsed);
            }
        }

        private void OnSystemSuspending(object sender, EventArgs e)
        {
            if (CurrentState == RecordingState.Recording || CurrentState == RecordingState.Paused)
            {
                _logger.Warning("System suspending during recording, stopping...");
                StopRecording();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _powerEventsService.StopMonitoring();
                _captureService.StateChanged -= OnCaptureStateChanged;
                _captureService.Error -= OnCaptureError;
                _captureService.ProgressUpdated -= OnProgressUpdated;

                StopTimers();
                CleanupSession();

                _disposed = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error disposing RecordingController");
            }
        }
    }
}
