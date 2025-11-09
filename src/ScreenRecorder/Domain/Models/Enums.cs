namespace ScreenRecorder.Domain.Models
{
    public enum RecordingMode
    {
        FullScreen,
        Window,
        Region,
        MultiMonitor
    }

    public enum RecordingState
    {
        Idle,
        Starting,
        Recording,
        Paused,
        Stopping,
        Finalizing,
        Error
    }

    public enum AudioSource
    {
        None,
        SystemAudio,
        Microphone,
        Both
    }

    public enum VideoCodec
    {
        H264,
        H265,
        FFV1
    }

    public enum AudioCodec
    {
        AAC,
        MP3,
        PCM
    }

    public enum ContainerFormat
    {
        MP4,
        MKV,
        AVI
    }

    public enum BitrateMode
    {
        Auto,
        Custom
    }

    public enum CursorCaptureMode
    {
        None,
        Standard,
        WithHighlight
    }

    public enum ThemeMode
    {
        Light,
        Dark,
        HighContrast
    }

    public enum FrameRate
    {
        Fps24 = 24,
        Fps30 = 30,
        Fps60 = 60,
        Fps120 = 120
    }

    public enum QualityPreset
    {
        Low,
        Medium,
        High,
        VeryHigh,
        Lossless
    }

    public enum HotkeyAction
    {
        StartStop,
        Pause,
        ToggleMicrophone,
        DropMarker,
        CancelRecording
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
