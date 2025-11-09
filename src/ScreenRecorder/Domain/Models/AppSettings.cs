using System.Collections.Generic;

namespace ScreenRecorder.Domain.Models
{
    public class AppSettings
    {
        public string Version { get; set; }
        public System.DateTime ModifiedDate { get; set; }
        public GeneralSettings General { get; set; }
        public StorageSettings Storage { get; set; }
        public List<RecordingProfile> Profiles { get; set; }
        public string ActiveProfileId { get; set; }
        public Dictionary<HotkeyAction, Hotkey> Hotkeys { get; set; }
        public UISettings UI { get; set; }
        public UpdateSettings Updates { get; set; }

        public AppSettings()
        {
            Version = "1.0.0";
            General = new GeneralSettings();
            Storage = new StorageSettings();
            Profiles = new List<RecordingProfile>();
            Hotkeys = new Dictionary<HotkeyAction, Hotkey>();
            UI = new UISettings();
            Updates = new UpdateSettings();
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // Add default profiles
            if (Profiles.Count == 0)
            {
                var quickProfile = RecordingProfile.CreateQuick1080p30();
                quickProfile.IsDefault = true;
                Profiles.Add(quickProfile);
                Profiles.Add(RecordingProfile.CreateGameplay1080p60());
                Profiles.Add(RecordingProfile.CreateLosslessShort());
                ActiveProfileId = quickProfile.Id;
            }

            // Add default hotkeys
            if (Hotkeys.Count == 0)
            {
                Hotkeys[HotkeyAction.StartStop] = new Hotkey(
                    HotkeyAction.StartStop,
                    System.Windows.Input.Key.F9,
                    System.Windows.Input.ModifierKeys.None);

                Hotkeys[HotkeyAction.Pause] = new Hotkey(
                    HotkeyAction.Pause,
                    System.Windows.Input.Key.F10,
                    System.Windows.Input.ModifierKeys.None);

                Hotkeys[HotkeyAction.ToggleMicrophone] = new Hotkey(
                    HotkeyAction.ToggleMicrophone,
                    System.Windows.Input.Key.F11,
                    System.Windows.Input.ModifierKeys.None);

                Hotkeys[HotkeyAction.DropMarker] = new Hotkey(
                    HotkeyAction.DropMarker,
                    System.Windows.Input.Key.F12,
                    System.Windows.Input.ModifierKeys.None);
            }
        }
    }

    public class GeneralSettings
    {
        public bool StartWithWindows { get; set; }
        public bool StartMinimized { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool ShowNotifications { get; set; }
        public string Language { get; set; }

        public GeneralSettings()
        {
            StartWithWindows = false;
            StartMinimized = false;
            MinimizeToTray = true;
            ShowNotifications = true;
            Language = "en-US";
        }
    }

    public class StorageSettings
    {
        public string OutputDirectory { get; set; }
        public string FilenameTemplate { get; set; }
        public bool OrganizeByDate { get; set; }
        public bool PortableMode { get; set; }
        public int MaxRecoveryFiles { get; set; }

        public StorageSettings()
        {
            OutputDirectory = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyVideos),
                "Recordings");
            FilenameTemplate = "clip_{timestamp}";
            OrganizeByDate = true;
            PortableMode = false;
            MaxRecoveryFiles = 5;
        }
    }

    public class UISettings
    {
        public ThemeMode Theme { get; set; }
        public bool ShowVUMeters { get; set; }
        public bool ShowFrameRate { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public bool RememberWindowPosition { get; set; }

        public UISettings()
        {
            Theme = ThemeMode.Light;
            ShowVUMeters = true;
            ShowFrameRate = true;
            WindowWidth = 800;
            WindowHeight = 600;
            RememberWindowPosition = true;
        }
    }

    public class UpdateSettings
    {
        public bool AutoCheckForUpdates { get; set; }
        public bool DownloadUpdatesAutomatically { get; set; }
        public string UpdateChannel { get; set; }

        public UpdateSettings()
        {
            AutoCheckForUpdates = true;
            DownloadUpdatesAutomatically = false;
            UpdateChannel = "stable";
        }
    }
}
