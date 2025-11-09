using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AForge.Video.FFMPEG;
using ScreenRecorder.Domain.Models;
using Serilog;

namespace ScreenRecorder.Infrastructure.Recording
{
    public class ScreenCaptureService : IScreenCaptureService, IDisposable
    {
        private readonly ILogger _logger;
        private RecordingSession _currentSession;
        private RecordingState _currentState;
        private VideoFileWriter _videoWriter;
        private Thread _recordingThread;
        private bool _isRecording;
        private bool _isPaused;
        private DateTime _pauseStartTime;
        private TimeSpan _totalPausedTime;
        private CancellationTokenSource _cancellationTokenSource;

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
                _isRecording = true;
                _isPaused = false;

                _logger.Information("Starting recording session {Id}", session.Id);

                // Get capture settings
                var profile = session.Profile;
                var bounds = GetCaptureBounds(profile.CaptureTarget);
                var frameRate = (int)profile.OutputFormat.FrameRate;
                var bitRate = profile.OutputFormat.VideoBitrateMbps * 1000 * 1000;

                // Initialize video writer
                _videoWriter = new VideoFileWriter();

                // Determine codec
                var codec = GetVideoCodec(profile.OutputFormat.VideoCodec);

                // Open video file
                _videoWriter.Open(
                    session.TempFilePath,
                    bounds.Width,
                    bounds.Height,
                    frameRate,
                    codec,
                    bitRate);

                // Start recording thread
                _cancellationTokenSource = new CancellationTokenSource();
                _recordingThread = new Thread(() => RecordingLoop(bounds, frameRate, _cancellationTokenSource.Token))
                {
                    Name = "ScreenRecorder",
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal
                };

                session.StartTime = DateTime.Now;
                _recordingThread.Start();

                CurrentState = RecordingState.Recording;
                _logger.Information("Recording started successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start recording");
                CurrentState = RecordingState.Error;
                if (_currentSession != null)
                    _currentSession.ErrorMessage = ex.Message;
                Error?.Invoke(this, ex.Message);
                CleanupWriter();
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

                _isPaused = true;
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

                _totalPausedTime += DateTime.Now - _pauseStartTime;
                _isPaused = false;
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

                _isRecording = false;
                _cancellationTokenSource?.Cancel();

                // Wait for recording thread to finish
                if (_recordingThread != null && _recordingThread.IsAlive)
                {
                    if (!_recordingThread.Join(5000)) // Wait up to 5 seconds
                    {
                        _logger.Warning("Recording thread did not finish in time");
                    }
                }

                CleanupWriter();

                if (_currentSession != null)
                {
                    _currentSession.EndTime = DateTime.Now;
                    _currentSession.PausedDuration = _totalPausedTime;
                    _currentSession.Duration = _currentSession.EndTime.Value - _currentSession.StartTime;
                }

                CurrentState = RecordingState.Idle;
                _logger.Information("Recording stopped");
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
            // AForge.Video.FFMPEG doesn't directly expose hardware acceleration
            // but FFMPEG itself supports it if configured
            return false;
        }

        public void CheckMediaFoundationAvailability()
        {
            try
            {
                // Check if we can create bitmaps for screen capture
                using (var bmp = new Bitmap(1, 1))
                {
                    _logger.Information("GDI+ available for screen capture");
                }

                // Check if FFMPEG is available
                var testWriter = new VideoFileWriter();
                _logger.Information("FFMPEG video writer available");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Video recording not available");
                throw new InvalidOperationException(
                    "Video recording capabilities not available. FFMPEG may not be properly installed.", ex);
            }
        }

        private void RecordingLoop(Rectangle bounds, int frameRate, CancellationToken cancellationToken)
        {
            var frameInterval = TimeSpan.FromSeconds(1.0 / frameRate);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var frameCount = 0;
            var lastProgressUpdate = DateTime.Now;

            try
            {
                while (_isRecording && !cancellationToken.IsCancellationRequested)
                {
                    if (_isPaused)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    var targetTime = TimeSpan.FromTicks(frameInterval.Ticks * frameCount);
                    var currentTime = stopwatch.Elapsed - _totalPausedTime;

                    if (currentTime >= targetTime)
                    {
                        CaptureFrame(bounds);
                        frameCount++;

                        // Report progress every second
                        if ((DateTime.Now - lastProgressUpdate).TotalSeconds >= 1.0)
                        {
                            var elapsed = DateTime.Now - _currentSession.StartTime - _totalPausedTime;
                            ProgressUpdated?.Invoke(this, elapsed);
                            lastProgressUpdate = DateTime.Now;
                        }
                    }
                    else
                    {
                        // Sleep until next frame (but not too long to stay responsive)
                        var sleepTime = targetTime - currentTime;
                        if (sleepTime.TotalMilliseconds > 1)
                        {
                            Thread.Sleep(1);
                        }
                        else
                        {
                            Thread.SpinWait(100); // Spin for very short waits
                        }
                    }
                }

                _logger.Information("Recording loop completed. Captured {FrameCount} frames", frameCount);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in recording loop");
                Error?.Invoke(this, ex.Message);
                CurrentState = RecordingState.Error;
            }
        }

        private void CaptureFrame(Rectangle bounds)
        {
            Bitmap bitmap = null;
            try
            {
                // Capture screen
                bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);

                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                // Write frame to video
                lock (_videoWriter)
                {
                    if (_videoWriter != null && _isRecording)
                    {
                        _videoWriter.WriteVideoFrame(bitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to capture frame");
            }
            finally
            {
                bitmap?.Dispose();
            }
        }

        private Rectangle GetCaptureBounds(CaptureTarget target)
        {
            try
            {
                switch (target.Mode)
                {
                    case RecordingMode.FullScreen:
                        var screenIndex = Math.Min(target.MonitorIndex, Screen.AllScreens.Length - 1);
                        var screen = Screen.AllScreens[screenIndex];
                        return screen.Bounds;

                    case RecordingMode.Window:
                        // Get window bounds
                        if (target.WindowHandle != IntPtr.Zero && GetWindowRect(target.WindowHandle, out var rect))
                        {
                            return new Rectangle(rect.Left, rect.Top,
                                rect.Right - rect.Left, rect.Bottom - rect.Top);
                        }
                        // Fallback to primary screen
                        _logger.Warning("Could not get window bounds, using primary screen");
                        return Screen.PrimaryScreen.Bounds;

                    case RecordingMode.Region:
                        if (target.Region.Width > 0 && target.Region.Height > 0)
                        {
                            return target.Region;
                        }
                        _logger.Warning("Invalid region, using primary screen");
                        return Screen.PrimaryScreen.Bounds;

                    default:
                        return Screen.PrimaryScreen.Bounds;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting capture bounds, using primary screen");
                return Screen.PrimaryScreen.Bounds;
            }
        }

        private VideoCodec GetVideoCodec(Domain.Models.VideoCodec codec)
        {
            // Map our codec enum to AForge VideoCodec
            switch (codec)
            {
                case Domain.Models.VideoCodec.H264:
                    return VideoCodec.H264;

                case Domain.Models.VideoCodec.H265:
                    // H265 might not be supported, fallback to H264
                    _logger.Warning("H265 requested but may not be available, using H264");
                    return VideoCodec.MPEG4;

                case Domain.Models.VideoCodec.FFV1:
                    // FFV1 lossless codec
                    return VideoCodec.Raw;

                default:
                    return VideoCodec.MPEG4; // Default to MPEG4
            }
        }

        private void CleanupWriter()
        {
            try
            {
                if (_videoWriter != null)
                {
                    lock (_videoWriter)
                    {
                        _videoWriter.Close();
                        _videoWriter.Dispose();
                        _videoWriter = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error cleaning up video writer");
            }
        }

        public void Dispose()
        {
            try
            {
                if (_isRecording)
                {
                    StopRecording();
                }

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                CleanupWriter();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error disposing ScreenCaptureService");
            }
        }

        // P/Invoke for window capture
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
