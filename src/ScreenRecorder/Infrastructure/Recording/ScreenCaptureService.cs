using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using ScreenRecorder.Domain.Models;
using SharpAvi;
using SharpAvi.Codecs;
using SharpAvi.Output;
using Serilog;

namespace ScreenRecorder.Infrastructure.Recording
{
    public class ScreenCaptureService : IScreenCaptureService, IDisposable
    {
        private readonly ILogger _logger;
        private RecordingSession _currentSession;
        private RecordingState _currentState;
        private AviWriter _writer;
        private IAviVideoStream _videoStream;
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

                // Initialize AVI writer
                var profile = session.Profile;
                var frameRate = (int)profile.OutputFormat.FrameRate;

                _writer = new AviWriter(session.TempFilePath)
                {
                    FramesPerSecond = frameRate,
                    EmitIndex1 = true
                };

                // Create video stream
                var encoder = GetVideoEncoder(profile.OutputFormat);
                var bounds = GetCaptureBounds(profile.CaptureTarget);

                _videoStream = _writer.AddVideoStream();
                _videoStream.Width = bounds.Width;
                _videoStream.Height = bounds.Height;
                _videoStream.Codec = encoder;
                _videoStream.BitsPerPixel = BitsPerPixel.Bpp32;

                // Start recording thread
                _cancellationTokenSource = new CancellationTokenSource();
                _recordingThread = new Thread(() => RecordingLoop(bounds, frameRate, _cancellationTokenSource.Token))
                {
                    Name = "ScreenRecorder",
                    IsBackground = true
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
                    _recordingThread.Join(5000); // Wait up to 5 seconds
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
            // SharpAvi doesn't use hardware acceleration directly
            // This would need to be implemented with Media Foundation or other APIs
            return false;
        }

        public void CheckMediaFoundationAvailability()
        {
            try
            {
                // Basic check - try to create a bitmap
                using (var bmp = new Bitmap(1, 1))
                {
                    _logger.Information("GDI+ available for screen capture");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Screen capture not available");
                throw new InvalidOperationException(
                    "Screen capture capabilities not available on this system.", ex);
            }
        }

        private void RecordingLoop(Rectangle bounds, int frameRate, CancellationToken cancellationToken)
        {
            var frameInterval = TimeSpan.FromSeconds(1.0 / frameRate);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var frameCount = 0;

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

                        // Report progress
                        var elapsed = DateTime.Now - _currentSession.StartTime - _totalPausedTime;
                        ProgressUpdated?.Invoke(this, elapsed);
                    }
                    else
                    {
                        // Sleep until next frame
                        var sleepTime = targetTime - currentTime;
                        if (sleepTime.TotalMilliseconds > 1)
                        {
                            Thread.Sleep(1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in recording loop");
                Error?.Invoke(this, ex.Message);
            }
        }

        private void CaptureFrame(Rectangle bounds)
        {
            try
            {
                using (var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppRgb))
                {
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    }

                    // Write frame to video stream
                    var bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppRgb);

                    try
                    {
                        var frameData = new byte[bitmapData.Stride * bitmapData.Height];
                        Marshal.Copy(bitmapData.Scan0, frameData, 0, frameData.Length);
                        _videoStream.WriteFrame(true, frameData, 0, frameData.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to capture frame");
            }
        }

        private Rectangle GetCaptureBounds(CaptureTarget target)
        {
            switch (target.Mode)
            {
                case RecordingMode.FullScreen:
                    var screen = Screen.AllScreens[Math.Min(target.MonitorIndex, Screen.AllScreens.Length - 1)];
                    return screen.Bounds;

                case RecordingMode.Window:
                    // Get window bounds
                    if (GetWindowRect(target.WindowHandle, out var rect))
                    {
                        return new Rectangle(rect.Left, rect.Top,
                            rect.Right - rect.Left, rect.Bottom - rect.Top);
                    }
                    // Fallback to primary screen
                    return Screen.PrimaryScreen.Bounds;

                case RecordingMode.Region:
                    return target.Region;

                default:
                    return Screen.PrimaryScreen.Bounds;
            }
        }

        private FourCC GetVideoEncoder(OutputFormat format)
        {
            // Use Motion JPEG as default codec (widely supported)
            // For production with H.264, you'd need to install/include x264 or use Media Foundation
            return KnownFourCCs.Codecs.MotionJpeg;
        }

        private void CleanupWriter()
        {
            try
            {
                _videoStream?.Dispose();
                _videoStream = null;

                _writer?.Close();
                _writer = null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error cleaning up AVI writer");
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
