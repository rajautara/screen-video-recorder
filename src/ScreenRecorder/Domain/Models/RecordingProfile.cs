using System;

namespace ScreenRecorder.Domain.Models
{
    public class RecordingProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public CaptureTarget CaptureTarget { get; set; }
        public OutputFormat OutputFormat { get; set; }
        public AudioMix AudioMix { get; set; }
        public CursorCaptureMode CursorMode { get; set; }
        public bool ShowClickHighlight { get; set; }
        public bool ShowCountdown { get; set; }
        public int CountdownSeconds { get; set; }
        public bool PlayBeep { get; set; }
        public int AutoStopSeconds { get; set; }
        public int AutoSplitMinutes { get; set; }
        public long AutoSplitSizeMB { get; set; }
        public bool IsDefault { get; set; }
        public bool IsReadOnly { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public RecordingProfile()
        {
            Id = Guid.NewGuid().ToString();
            Name = "New Profile";
            CaptureTarget = new CaptureTarget();
            OutputFormat = new OutputFormat();
            AudioMix = new AudioMix();
            CursorMode = CursorCaptureMode.Standard;
            ShowCountdown = true;
            CountdownSeconds = 3;
            PlayBeep = false;
            AutoStopSeconds = 0;
            AutoSplitMinutes = 0;
            AutoSplitSizeMB = 0;
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
        }

        public RecordingProfile Clone()
        {
            return new RecordingProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = Name + " (Copy)",
                Description = Description,
                CaptureTarget = CaptureTarget?.Clone(),
                OutputFormat = OutputFormat?.Clone(),
                AudioMix = AudioMix?.Clone(),
                CursorMode = CursorMode,
                ShowClickHighlight = ShowClickHighlight,
                ShowCountdown = ShowCountdown,
                CountdownSeconds = CountdownSeconds,
                PlayBeep = PlayBeep,
                AutoStopSeconds = AutoStopSeconds,
                AutoSplitMinutes = AutoSplitMinutes,
                AutoSplitSizeMB = AutoSplitSizeMB,
                IsDefault = false,
                IsReadOnly = false,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };
        }

        public static RecordingProfile CreateQuick1080p30()
        {
            return new RecordingProfile
            {
                Name = "Quick 1080p/30",
                Description = "Standard quality recording at 30 FPS",
                IsReadOnly = true,
                OutputFormat = new OutputFormat
                {
                    FrameRate = FrameRate.Fps30,
                    VideoBitrateMbps = 8,
                    QualityPreset = QualityPreset.High
                },
                CursorMode = CursorCaptureMode.Standard,
                AudioMix = new AudioMix { Source = AudioSource.Both }
            };
        }

        public static RecordingProfile CreateGameplay1080p60()
        {
            return new RecordingProfile
            {
                Name = "Gameplay 1080p/60",
                Description = "High framerate for smooth gameplay recording",
                IsReadOnly = true,
                OutputFormat = new OutputFormat
                {
                    FrameRate = FrameRate.Fps60,
                    VideoBitrateMbps = 16,
                    QualityPreset = QualityPreset.VeryHigh,
                    HardwareAcceleration = true
                },
                CursorMode = CursorCaptureMode.Standard,
                AudioMix = new AudioMix { Source = AudioSource.SystemAudio }
            };
        }

        public static RecordingProfile CreateLosslessShort()
        {
            return new RecordingProfile
            {
                Name = "Lossless Short Clip",
                Description = "Maximum quality for short recordings",
                IsReadOnly = true,
                OutputFormat = new OutputFormat
                {
                    VideoCodec = VideoCodec.FFV1,
                    Container = ContainerFormat.MKV,
                    FrameRate = FrameRate.Fps60,
                    QualityPreset = QualityPreset.Lossless
                },
                AutoStopSeconds = 60,
                CursorMode = CursorCaptureMode.Standard
            };
        }
    }
}
