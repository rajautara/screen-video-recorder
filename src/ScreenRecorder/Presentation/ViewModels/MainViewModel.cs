using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ScreenRecorder.Domain.Models;
using ScreenRecorder.Domain.Orchestration;
using ScreenRecorder.Infrastructure.Audio;
using ScreenRecorder.Infrastructure.Hotkeys;
using ScreenRecorder.Infrastructure.Settings;
using ScreenRecorder.Presentation.Services;
using Serilog;

namespace ScreenRecorder.Presentation.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ILogger _logger;
        private readonly IRecordingController _recordingController;
        private readonly ISettingsService _settingsService;
        private readonly IAudioDeviceService _audioDeviceService;
        private readonly IGlobalHotkeyService _hotkeyService;
        private readonly IDialogService _dialogService;
        private readonly INotificationService _notificationService;
        private readonly ITrayService _trayService;

        private RecordingState _currentState;
        private string _statusText;
        private string _durationText;
        private RecordingProfile _selectedProfile;
        private bool _isRecording;
        private bool _isPaused;
        private float _systemAudioLevel;
        private float _microphoneLevel;

        public MainViewModel(
            ILogger logger,
            IRecordingController recordingController,
            ISettingsService settingsService,
            IAudioDeviceService audioDeviceService,
            IGlobalHotkeyService hotkeyService,
            IDialogService dialogService,
            INotificationService notificationService,
            ITrayService trayService)
        {
            _logger = logger;
            _recordingController = recordingController;
            _settingsService = settingsService;
            _audioDeviceService = audioDeviceService;
            _hotkeyService = hotkeyService;
            _dialogService = dialogService;
            _notificationService = notificationService;
            _trayService = trayService;

            Initialize();
        }

        #region Properties

        public RecordingState CurrentState
        {
            get => _currentState;
            set
            {
                if (SetProperty(ref _currentState, value))
                {
                    UpdateStatusText();
                    UpdateButtonStates();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string DurationText
        {
            get => _durationText;
            set => SetProperty(ref _durationText, value);
        }

        public RecordingProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (SetProperty(ref _selectedProfile, value))
                {
                    _recordingController.ActiveProfile = value;
                    SaveActiveProfile();
                }
            }
        }

        public bool IsRecording
        {
            get => _isRecording;
            set => SetProperty(ref _isRecording, value);
        }

        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }

        public float SystemAudioLevel
        {
            get => _systemAudioLevel;
            set => SetProperty(ref _systemAudioLevel, value);
        }

        public float MicrophoneLevel
        {
            get => _microphoneLevel;
            set => SetProperty(ref _microphoneLevel, value);
        }

        public ObservableCollection<RecordingProfile> Profiles { get; set; }

        #endregion

        #region Commands

        public ICommand StartRecordingCommand { get; private set; }
        public ICommand StopRecordingCommand { get; private set; }
        public ICommand PauseResumeCommand { get; private set; }
        public ICommand CancelRecordingCommand { get; private set; }
        public ICommand DropMarkerCommand { get; private set; }
        public ICommand OpenSettingsCommand { get; private set; }
        public ICommand SelectRegionCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }

        #endregion

        private void Initialize()
        {
            // Initialize commands
            StartRecordingCommand = new RelayCommand(StartRecording, CanStartRecording);
            StopRecordingCommand = new RelayCommand(StopRecording, CanStopRecording);
            PauseResumeCommand = new RelayCommand(PauseResume, CanPauseResume);
            CancelRecordingCommand = new RelayCommand(CancelRecording, CanCancelRecording);
            DropMarkerCommand = new RelayCommand(DropMarker, CanDropMarker);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            SelectRegionCommand = new RelayCommand(SelectRegion);
            ExitCommand = new RelayCommand(Exit);

            // Load profiles
            LoadProfiles();

            // Wire up events
            _recordingController.StateChanged += OnRecordingStateChanged;
            _recordingController.RecordingStarted += OnRecordingStarted;
            _recordingController.RecordingCompleted += OnRecordingCompleted;
            _recordingController.Error += OnRecordingError;
            _recordingController.ProgressUpdated += OnProgressUpdated;

            _hotkeyService.HotkeyPressed += OnHotkeyPressed;

            _trayService.Initialize();
            _trayService.TrayIconClicked += OnTrayIconClicked;
            _trayService.ExitRequested += OnExitRequested;

            // Register hotkeys
            RegisterHotkeys();

            // Set initial state
            CurrentState = RecordingState.Idle;
            StatusText = "Ready to record";
            DurationText = "00:00";

            _logger.Information("MainViewModel initialized");
        }

        private void LoadProfiles()
        {
            var settings = _settingsService.Current;
            Profiles = new ObservableCollection<RecordingProfile>(settings.Profiles);

            // Set active profile
            if (!string.IsNullOrEmpty(settings.ActiveProfileId))
            {
                SelectedProfile = Profiles.FirstOrDefault(p => p.Id == settings.ActiveProfileId);
            }

            if (SelectedProfile == null && Profiles.Count > 0)
            {
                SelectedProfile = Profiles[0];
            }
        }

        private void SaveActiveProfile()
        {
            var settings = _settingsService.Current;
            settings.ActiveProfileId = SelectedProfile?.Id;
            _settingsService.Save(settings);
        }

        private void RegisterHotkeys()
        {
            var settings = _settingsService.Current;
            foreach (var hotkey in settings.Hotkeys.Values)
            {
                _hotkeyService.RegisterHotkey(hotkey);
            }
        }

        #region Command Methods

        private bool CanStartRecording() => CurrentState == RecordingState.Idle;

        private void StartRecording()
        {
            try
            {
                _logger.Information("Starting recording from UI");
                _recordingController.StartRecording();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start recording");
                _dialogService.ShowMessage($"Failed to start recording:\n{ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanStopRecording() => CurrentState == RecordingState.Recording || CurrentState == RecordingState.Paused;

        private void StopRecording()
        {
            try
            {
                _logger.Information("Stopping recording from UI");
                _recordingController.StopRecording();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to stop recording");
            }
        }

        private bool CanPauseResume() => CurrentState == RecordingState.Recording || CurrentState == RecordingState.Paused;

        private void PauseResume()
        {
            try
            {
                if (CurrentState == RecordingState.Recording)
                {
                    _recordingController.PauseRecording();
                }
                else if (CurrentState == RecordingState.Paused)
                {
                    _recordingController.ResumeRecording();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to pause/resume recording");
            }
        }

        private bool CanCancelRecording() => CurrentState == RecordingState.Recording || CurrentState == RecordingState.Paused;

        private void CancelRecording()
        {
            if (_dialogService.ShowConfirmation("Are you sure you want to cancel this recording?", "Cancel Recording"))
            {
                _recordingController.CancelRecording();
            }
        }

        private bool CanDropMarker() => CurrentState == RecordingState.Recording;

        private void DropMarker()
        {
            _recordingController.DropMarker();
            _notificationService.ShowToast("Marker dropped");
        }

        private void OpenSettings()
        {
            // TODO: Open settings window
            _notificationService.ShowToast("Settings (not yet implemented)");
        }

        private void SelectRegion()
        {
            // TODO: Open region picker
            _notificationService.ShowToast("Region picker (not yet implemented)");
        }

        private void Exit()
        {
            if (CurrentState != RecordingState.Idle)
            {
                if (!_dialogService.ShowConfirmation("Recording is in progress. Are you sure you want to exit?", "Exit"))
                {
                    return;
                }
            }

            System.Windows.Application.Current.Shutdown();
        }

        #endregion

        #region Event Handlers

        private void OnRecordingStateChanged(object sender, RecordingState state)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentState = state;
            });
        }

        private void OnRecordingStarted(object sender, RecordingSession session)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsRecording = true;
                IsPaused = false;
                _notificationService.ShowToast("Recording started");
            });
        }

        private void OnRecordingCompleted(object sender, RecordingSession session)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsRecording = false;
                IsPaused = false;
                DurationText = "00:00";

                var message = $"Recording saved:\n{session.FinalFilePath}";
                _dialogService.ShowMessage(message, "Recording Complete",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            });
        }

        private void OnRecordingError(object sender, string error)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _dialogService.ShowMessage($"Recording error:\n{error}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            });
        }

        private void OnProgressUpdated(object sender, TimeSpan elapsed)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                DurationText = elapsed.ToString(@"hh\:mm\:ss");
            });
        }

        private void OnHotkeyPressed(object sender, HotkeyAction action)
        {
            _logger.Debug("Hotkey action triggered: {Action}", action);

            switch (action)
            {
                case HotkeyAction.StartStop:
                    if (CanStartRecording())
                        StartRecording();
                    else if (CanStopRecording())
                        StopRecording();
                    break;

                case HotkeyAction.Pause:
                    if (CanPauseResume())
                        PauseResume();
                    break;

                case HotkeyAction.DropMarker:
                    if (CanDropMarker())
                        DropMarker();
                    break;

                case HotkeyAction.CancelRecording:
                    if (CanCancelRecording())
                        CancelRecording();
                    break;
            }
        }

        private void OnTrayIconClicked(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    if (mainWindow.Visibility == System.Windows.Visibility.Visible)
                    {
                        mainWindow.Hide();
                    }
                    else
                    {
                        mainWindow.Show();
                        mainWindow.WindowState = System.Windows.WindowState.Normal;
                        mainWindow.Activate();
                    }
                }
            });
        }

        private void OnExitRequested(object sender, EventArgs e)
        {
            Exit();
        }

        #endregion

        private void UpdateStatusText()
        {
            StatusText = CurrentState switch
            {
                RecordingState.Idle => "Ready to record",
                RecordingState.Starting => "Starting...",
                RecordingState.Recording => "Recording...",
                RecordingState.Paused => "Paused",
                RecordingState.Stopping => "Stopping...",
                RecordingState.Finalizing => "Finalizing...",
                RecordingState.Error => "Error",
                _ => "Unknown"
            };
        }

        private void UpdateButtonStates()
        {
            IsRecording = CurrentState == RecordingState.Recording || CurrentState == RecordingState.Paused;
            IsPaused = CurrentState == RecordingState.Paused;

            CommandManager.InvalidateRequerySuggested();
        }
    }
}
