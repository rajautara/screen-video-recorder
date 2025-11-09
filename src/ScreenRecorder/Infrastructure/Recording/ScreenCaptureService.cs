using System;
using System.Runtime.InteropServices;
using ScreenRecorder.Domain.Models;
using ScreenRecorderLib;
using Serilog;

namespace ScreenRecorder.Infrastructure.Recording
{
    public class ScreenCaptureService : IScreenCaptureService, IDisposable
    {
        private readonly ILogger _logger;
        private Recorder _recorder;
        private RecordingSession _currentSession;
        private RecordingState _currentState;
        private DateTime _pauseStartTime;
        private TimeSpan _totalPausedTime;

        public RecordingState CurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    StateChanged?.Invoke(this, value);
                    _logger.Debug("Recording state changed to {State}", value);
                }
            }
        }

        public event EventHandler<RecordingState> StateChanged;
        public event EventHandler<string> Error;
        public event EventHandler<TimeSpan> ProgressUpdated;

        public ScreenCaptureService(ILogger logger)
        {
            _logger = logger;
            CurrentState = RecordingState.Idle;
        }

        public void StartRecording(RecordingSession session)
        {
            try
            {
                if (CurrentState != RecordingState.Idle)
                {
                    throw new InvalidOperationException($"Cannot start recording in {CurrentState} state");
                }

                CurrentState = RecordingState.Starting;
                _currentSession = session;
                _totalPausedTime = TimeSpan.Zero;

                _logger.Information("Starting recording session {Id}", session.Id);

                // Configure recorder options
                var options = CreateRecorderOptions(session);

                _recorder = Recorder.CreateRecorder(options);
                _recorder.OnRecordingComplete += OnRecordingComplete;
                _recorder.OnRecordingFailed += OnRecordingFailed;
                _recorder.OnStatusChanged += OnStatusChanged;

                // Start recording
                _recorder.Record(session.TempFilePath);

                session.StartTime = DateTime.Now;
                CurrentState = RecordingState.Recording;

                _logger.Information("Recording started successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start recording");
                CurrentState = RecordingState.Error;
                _currentSession.ErrorMessage = ex.Message;
                Error?.Invoke(this, ex.Message);
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

                _recorder?.Pause();
                _pauseStartTime = DateTime.Now;
                CurrentState = RecordingState.Paused;
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

                _recorder?.Resume();
                _totalPausedTime += DateTime.Now - _pauseStartTime;
                CurrentState = RecordingState.Recording;
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

                CurrentState = RecordingState.Stopping;
                _logger.Information("Stopping recording...");

                _recorder?.Stop();

                if (_currentSession != null)
                {
                    _currentSession.EndTime = DateTime.Now;
                    _currentSession.PausedDuration = _totalPausedTime;
                    _currentSession.Duration = _currentSession.EndTime.Value - _currentSession.StartTime;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to stop recording");
                CurrentState = RecordingState.Error;
                Error?.Invoke(this, ex.Message);
            }
        }

        public bool IsHardwareAccelerationAvailable()
        {
            // Check if hardware encoding is available
            try
            {
                return true; // ScreenRecorderLib will handle fallback automatically
            }
            catch
            {
                return false;
            }
        }

        public void CheckMediaFoundationAvailability()
        {
            try
            {
                // Try to create a simple recorder to test Media Foundation
                var testOptions = new RecorderOptions();
                using (var testRecorder = Recorder.CreateRecorder(testOptions))
                {
                    _logger.Information("Media Foundation is available");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Media Foundation not available");
                throw new InvalidOperationException(
                    "Media Foundation is not available on this system. " +
                    "If you're using Windows N edition, please install the Media Feature Pack.", ex);
            }
        }

        private RecorderOptions CreateRecorderOptions(RecordingSession session)
        {
            var profile = session.Profile;
            var options = new RecorderOptions
            {
                RecorderMode = RecorderMode.Video,
                IsThrottlingDisabled = false,
                IsHardwareEncodingEnabled = profile.OutputFormat.HardwareAcceleration,
                IsLowLatencyEnabled = false,
                IsMp4FastStartEnabled = true,
                AudioOptions = CreateAudioOptions(profile),
                VideoOptions = CreateVideoOptions(profile),
                MouseOptions = CreateMouseOptions(profile)
            };

            // Set source based on capture target
            if (profile.CaptureTarget.Mode == RecordingMode.FullScreen)
            {
                var displayOptions = new DisplayRecordingSource
                {
                    DisplayName = profile.CaptureTarget.DisplayDeviceName ?? GetPrimaryDisplay(),
                };
                options.SourceOptions = displayOptions;
            }
            else if (profile.CaptureTarget.Mode == RecordingMode.Window)
            {
                var windowOptions = new WindowRecordingSource
                {
                    Handle = profile.CaptureTarget.WindowHandle
                };
                options.SourceOptions = windowOptions;
            }
            else if (profile.CaptureTarget.Mode == RecordingMode.Region)
            {
                var region = profile.CaptureTarget.Region;
                var displayOptions = new DisplayRecordingSource
                {
                    DisplayName = GetPrimaryDisplay(),
                    RecordingArea = new RecordingArea
                    {
                        Left = region.X,
                        Top = region.Y,
                        Right = region.X + region.Width,
                        Bottom = region.Y + region.Height
                    }
                };
                options.SourceOptions = displayOptions;
            }

            return options;
        }

        private AudioOptions CreateAudioOptions(RecordingProfile profile)
        {
            var audioMix = profile.AudioMix;
            var options = new AudioOptions
            {
                IsAudioEnabled = audioMix.Source != AudioSource.None,
                IsOutputDeviceEnabled = audioMix.Source == AudioSource.SystemAudio || audioMix.Source == AudioSource.Both,
                IsInputDeviceEnabled = audioMix.Source == AudioSource.Microphone || audioMix.Source == AudioSource.Both,
                AudioOutputDevice = audioMix.SystemAudioDeviceId,
                AudioInputDevice = audioMix.MicrophoneDeviceId,
                OutputVolume = audioMix.SystemAudioVolume,
                InputVolume = audioMix.MicrophoneVolume
            };

            return options;
        }

        private VideoOptions CreateVideoOptions(RecordingProfile profile)
        {
            var format = profile.OutputFormat;
            var options = new VideoOptions
            {
                Framerate = (int)format.FrameRate,
                Quality = GetVideoQuality(format.QualityPreset),
                BitrateMode = format.BitrateMode == Domain.Models.BitrateMode.Auto
                    ? ScreenRecorderLib.BitrateControlMode.Quality
                    : ScreenRecorderLib.BitrateControlMode.UnconstrainedVBR,
                Bitrate = format.VideoBitrateMbps * 1000 * 1000, // Convert to bps
                IsFixedFramerate = true
            };

            return options;
        }

        private MouseOptions CreateMouseOptions(RecordingProfile profile)
        {
            return new MouseOptions
            {
                IsMousePointerEnabled = profile.CursorMode != CursorCaptureMode.None,
                IsMouseClicksDetected = profile.ShowClickHighlight,
                MouseClickDetectionColor = "#FFFF00",
                MouseClickDetectionRadius = 20,
                MouseClickDetectionDuration = 300
            };
        }

        private int GetVideoQuality(QualityPreset preset)
        {
            return preset switch
            {
                QualityPreset.Low => 50,
                QualityPreset.Medium => 65,
                QualityPreset.High => 75,
                QualityPreset.VeryHigh => 85,
                QualityPreset.Lossless => 100,
                _ => 75
            };
        }

        private string GetPrimaryDisplay()
        {
            return System.Windows.Forms.Screen.PrimaryScreen.DeviceName;
        }

        private void OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
        {
            _logger.Information("Recording completed: {Path}", e.FilePath);
            CurrentState = RecordingState.Idle;
        }

        private void OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            _logger.Error("Recording failed: {Error}", e.Error);
            CurrentState = RecordingState.Error;
            _currentSession.ErrorMessage = e.Error;
            Error?.Invoke(this, e.Error);
        }

        private void OnStatusChanged(object sender, RecordingStatusEventArgs e)
        {
            if (_currentSession != null && CurrentState == RecordingState.Recording)
            {
                var elapsed = DateTime.Now - _currentSession.StartTime - _totalPausedTime;
                _currentSession.Duration = elapsed;
                ProgressUpdated?.Invoke(this, elapsed);
            }
        }

        public void Dispose()
        {
            try
            {
                if (_recorder != null)
                {
                    _recorder.OnRecordingComplete -= OnRecordingComplete;
                    _recorder.OnRecordingFailed -= OnRecordingFailed;
                    _recorder.OnStatusChanged -= OnStatusChanged;
                    _recorder.Dispose();
                    _recorder = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error disposing ScreenCaptureService");
            }
        }
    }
}
