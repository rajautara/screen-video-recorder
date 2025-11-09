using ScreenRecorder.Domain.Models;
using System.Drawing;

namespace ScreenRecorder.Infrastructure.Settings
{
    /// <summary>
    /// Adapter that wraps ISettingsService to provide simplified IAppSettings interface
    /// </summary>
    public class AppSettingsAdapter : IAppSettings
    {
        private readonly ISettingsService _settingsService;
        private AppSettings _settings;

        public AppSettingsAdapter(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.Current;
        }

        public string OutputDirectory
        {
            get => _settings.Storage?.OutputDirectory ?? string.Empty;
            set
            {
                if (_settings.Storage != null)
                {
                    _settings.Storage.OutputDirectory = value;
                }
            }
        }

        public VideoCodec VideoCodec
        {
            get => GetActiveProfile()?.OutputFormat?.VideoCodec ?? VideoCodec.H264;
            set
            {
                var profile = GetActiveProfile();
                if (profile?.OutputFormat != null)
                {
                    profile.OutputFormat.VideoCodec = value;
                }
            }
        }

        public AudioCodec AudioCodec
        {
            get => GetActiveProfile()?.OutputFormat?.AudioCodec ?? AudioCodec.AAC;
            set
            {
                var profile = GetActiveProfile();
                if (profile?.OutputFormat != null)
                {
                    profile.OutputFormat.AudioCodec = value;
                }
            }
        }

        public int FrameRate
        {
            get => (int)(GetActiveProfile()?.OutputFormat?.FrameRate ?? Domain.Models.FrameRate.Fps30);
            set
            {
                var profile = GetActiveProfile();
                if (profile?.OutputFormat != null)
                {
                    profile.OutputFormat.FrameRate = (Domain.Models.FrameRate)value;
                }
            }
        }

        public string AudioDeviceId { get; set; }
        public bool IsSystemAudioEnabled { get; set; }
        public bool IsMicrophoneEnabled { get; set; }
        public Rectangle LastSelectedRegion { get; set; }

        public void Save()
        {
            _settingsService.Save(_settings);
        }

        public void Load()
        {
            _settings = _settingsService.Load();
        }

        private RecordingProfile GetActiveProfile()
        {
            if (string.IsNullOrEmpty(_settings.ActiveProfileId))
                return null;

            return _settings.Profiles?.Find(p => p.Id == _settings.ActiveProfileId);
        }
    }
}
