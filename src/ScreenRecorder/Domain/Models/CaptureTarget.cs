using System;
using System.Drawing;

namespace ScreenRecorder.Domain.Models
{
    public class CaptureTarget
    {
        public RecordingMode Mode { get; set; }
        public string DisplayDeviceName { get; set; }
        public IntPtr WindowHandle { get; set; }
        public string WindowTitle { get; set; }
        public Rectangle Region { get; set; }
        public bool FollowWindow { get; set; }
        public int MonitorIndex { get; set; }

        public CaptureTarget()
        {
            Mode = RecordingMode.FullScreen;
            MonitorIndex = 0;
            FollowWindow = true;
        }

        public static CaptureTarget CreateFullScreen(int monitorIndex = 0)
        {
            return new CaptureTarget
            {
                Mode = RecordingMode.FullScreen,
                MonitorIndex = monitorIndex
            };
        }

        public static CaptureTarget CreateWindow(IntPtr handle, string title)
        {
            return new CaptureTarget
            {
                Mode = RecordingMode.Window,
                WindowHandle = handle,
                WindowTitle = title,
                FollowWindow = true
            };
        }

        public static CaptureTarget CreateRegion(Rectangle region)
        {
            return new CaptureTarget
            {
                Mode = RecordingMode.Region,
                Region = region
            };
        }

        public CaptureTarget Clone()
        {
            return new CaptureTarget
            {
                Mode = Mode,
                DisplayDeviceName = DisplayDeviceName,
                WindowHandle = WindowHandle,
                WindowTitle = WindowTitle,
                Region = Region,
                FollowWindow = FollowWindow,
                MonitorIndex = MonitorIndex
            };
        }
    }
}
