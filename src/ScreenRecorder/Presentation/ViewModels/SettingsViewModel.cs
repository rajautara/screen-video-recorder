using System;
using ScreenRecorder.Domain.Models;
using ScreenRecorder.Infrastructure.Settings;
using System.ComponentModel;
using System.Windows.Input;
using Serilog;

namespace ScreenRecorder.Presentation.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IAppSettings _settings;
        private readonly ILogger _logger;

        public SettingsViewModel(IAppSettings settings, ILogger logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize properties from settings
            OutputDirectory = _settings.OutputDirectory;
            VideoCodec = _settings.VideoCodec;
            AudioCodec = _settings.AudioCodec;
            FrameRate = _settings.FrameRate;
            AudioDeviceId = _settings.AudioDeviceId;
            IsSystemAudioEnabled = _settings.IsSystemAudioEnabled;
            IsMicrophoneEnabled = _settings.IsMicrophoneEnabled;

            // Setup commands
            SaveCommand = new RelayCommand(SaveSettings);
            CancelCommand = new RelayCommand(Cancel);
            BrowseOutputDirectoryCommand = new RelayCommand(BrowseOutputDirectory);
        }

        private string _outputDirectory;
        public string OutputDirectory
        {
            get => _outputDirectory;
            set => SetProperty(ref _outputDirectory, value);
        }

        private VideoCodec _videoCodec;
        public VideoCodec VideoCodec
        {
            get => _videoCodec;
            set => SetProperty(ref _videoCodec, value);
        }

        private AudioCodec _audioCodec;
        public AudioCodec AudioCodec
        {
            get => _audioCodec;
            set => SetProperty(ref _audioCodec, value);
        }

        private int _frameRate;
        public int FrameRate
        {
            get => _frameRate;
            set => SetProperty(ref _frameRate, value);
        }

        private string _audioDeviceId;
        public string AudioDeviceId
        {
            get => _audioDeviceId;
            set => SetProperty(ref _audioDeviceId, value);
        }

        private bool _isSystemAudioEnabled;
        public bool IsSystemAudioEnabled
        {
            get => _isSystemAudioEnabled;
            set => SetProperty(ref _isSystemAudioEnabled, value);
        }

        private bool _isMicrophoneEnabled;
        public bool IsMicrophoneEnabled
        {
            get => _isMicrophoneEnabled;
            set => SetProperty(ref _isMicrophoneEnabled, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseOutputDirectoryCommand { get; }

        private void SaveSettings()
        {
            try
            {
                // Save settings
                _settings.OutputDirectory = OutputDirectory;
                _settings.VideoCodec = VideoCodec;
                _settings.AudioCodec = AudioCodec;
                _settings.FrameRate = FrameRate;
                _settings.AudioDeviceId = AudioDeviceId;
                _settings.IsSystemAudioEnabled = IsSystemAudioEnabled;
                _settings.IsMicrophoneEnabled = IsMicrophoneEnabled;

                _settings.Save();
                _logger.Information("Settings saved successfully");

                // Close the settings window or navigate back
                OnRequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save settings");
                // Show error message to user
            }
        }

        private void Cancel()
        {
            OnRequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void BrowseOutputDirectory()
        {
            // Implement directory browsing logic
            // This would typically open a folder browser dialog
        }

        public event EventHandler OnRequestClose;
    }
}
