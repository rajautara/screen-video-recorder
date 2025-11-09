using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Serilog;

namespace ScreenRecorder.Infrastructure.System
{
    public class DpiService : IDpiService
    {
        private readonly ILogger _logger;

        public DpiService(ILogger logger)
        {
            _logger = logger;
            SetDpiAwareness();
        }

        public void SetDpiAwareness()
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Process_Per_Monitor_DPI_Aware);
                    _logger.Information("DPI awareness set to Per-Monitor");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to set DPI awareness");
            }
        }

        public double GetDpiScale()
        {
            var dpiScale = 1.0;

            try
            {
                using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
                {
                    dpiScale = graphics.DpiX / 96.0;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get DPI scale");
            }

            return dpiScale;
        }

        public double GetDpiScaleForWindow(Window window)
        {
            try
            {
                var source = PresentationSource.FromVisual(window);
                if (source?.CompositionTarget != null)
                {
                    return source.CompositionTarget.TransformToDevice.M11;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get DPI scale for window");
            }

            return 1.0;
        }

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS value);

        private enum PROCESS_DPI_AWARENESS
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }
    }

    // Graphics helper for GDI+ DPI detection
    internal class Graphics : IDisposable
    {
        private IntPtr _hdc;

        private Graphics(IntPtr hdc)
        {
            _hdc = hdc;
        }

        public static Graphics FromHwnd(IntPtr hwnd)
        {
            var hdc = GetDC(hwnd);
            return new Graphics(hdc);
        }

        public int DpiX => GetDeviceCaps(_hdc, LOGPIXELSX);
        public int DpiY => GetDeviceCaps(_hdc, LOGPIXELSY);

        public void Dispose()
        {
            if (_hdc != IntPtr.Zero)
            {
                ReleaseDC(IntPtr.Zero, _hdc);
                _hdc = IntPtr.Zero;
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;
    }
}
