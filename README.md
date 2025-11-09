# Screen Recorder

A lightweight, high-performance Windows screen and video recorder built with WPF and .NET Framework 4.8.

## Features

### Core Recording Capabilities

- **Multiple Recording Modes**
  - Full screen capture (single or multi-monitor)
  - Window capture with automatic tracking
  - Region capture with resizable overlay
  - Per-monitor DPI scaling support

- **High-Quality Output**
  - H.264/H.265/FFV1 video codecs
  - MP4/MKV/AVI container formats
  - Configurable frame rates: 24/30/60/120 FPS
  - Bitrate presets: 4/8/12/20 Mbps
  - Hardware acceleration support

- **Advanced Audio**
  - System audio (loopback) via WASAPI
  - Microphone input with device selection
  - Audio mixing with independent volume control
  - Real-time VU meters
  - Auto device-change handling

- **Professional Controls**
  - Global hotkeys (customizable)
  - System tray integration
  - Pause/resume functionality
  - Time markers during recording
  - Auto-stop timer
  - Auto-split by duration or file size

### User Experience

- **Recording Profiles**
  - "Quick 1080p/30" - Standard quality recording
  - "Gameplay 1080p/60" - High framerate for smooth gameplay
  - "Lossless Short Clip" - Maximum quality for short recordings
  - Custom profiles with full configuration

- **Reliability**
  - Crash recovery system
  - Automatic temp file management
  - Safe finalization process
  - Watchdog for encoder failures
  - Windows N edition detection

- **Interface**
  - Modern WPF UI with MVVM architecture
  - Light/Dark/High-Contrast themes
  - DPI-aware rendering
  - Countdown timer with optional beep
  - Post-recording actions (open folder, rename, etc.)

## Architecture

### High-Level Design

```
┌─────────────────────────────────────────────┐
│          Presentation Layer (WPF)           │
│  Views | ViewModels | UI Services           │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│          Domain Layer                        │
│  Models | RecordingController (Orchestration)│
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│          Infrastructure Layer                │
│  Recording | Audio | Hotkeys | Storage      │
│  Settings | Logging | System                │
└─────────────────────────────────────────────┘
```

### Key Components

#### Domain Models
- **RecordingProfile** - Complete recording configuration
- **CaptureTarget** - What to record (screen, window, region)
- **OutputFormat** - Video/audio encoding settings
- **AudioMix** - Audio source and mixing configuration
- **AppSettings** - Application-wide settings
- **RecordingSession** - Runtime session state

#### Infrastructure Services
- **ScreenCaptureService** - Wraps ScreenRecorderLib for video capture
- **AudioDeviceService** - WASAPI audio device management
- **GlobalHotkeyService** - System-wide hotkey registration
- **StorageService** - File management and crash recovery
- **SettingsService** - JSON settings persistence
- **LogService** - Serilog-based logging

#### Orchestration
- **RecordingController** - State machine coordinator
  - States: Idle → Starting → Recording → Paused → Stopping → Finalizing
  - Handles auto-stop, auto-split, and recovery

#### UI Services
- **DialogService** - Modal dialogs
- **NotificationService** - Toast notifications
- **TrayService** - System tray integration
- **ThemeService** - Theme management
- **DpiService** - DPI scaling

## Project Structure

```
ScreenRecorder/
├── src/
│   └── ScreenRecorder/
│       ├── Domain/
│       │   ├── Models/           # Business entities
│       │   └── Orchestration/    # RecordingController
│       ├── Infrastructure/
│       │   ├── Recording/        # ScreenCaptureService
│       │   ├── Audio/            # AudioDeviceService
│       │   ├── Hotkeys/          # GlobalHotkeyService
│       │   ├── Storage/          # StorageService
│       │   ├── Settings/         # SettingsService
│       │   ├── Logging/          # LogService
│       │   └── System/           # DpiService, PowerEventsService
│       ├── Presentation/
│       │   ├── Views/            # WPF Windows/UserControls
│       │   ├── ViewModels/       # MVVM ViewModels
│       │   ├── Services/         # UI Services
│       │   └── Converters/       # Value converters
│       └── Resources/
│           ├── Themes/           # Light/Dark themes
│           ├── Strings/          # Localization
│           └── Icons/            # Application icons
├── ScreenRecorder.sln
└── README.md
```

## Dependencies

### NuGet Packages
- **SimpleInjector** (5.4.1) - Dependency injection
- **Serilog** (3.1.1) - Logging framework
- **Serilog.Sinks.File** (5.0.0) - File logging
- **Newtonsoft.Json** (13.0.3) - JSON serialization
- **ScreenRecorderLib** (5.0.63) - Screen recording engine
- **NAudio** (2.2.1) - Audio device management
- **Squirrel.Windows** (2.0.1) - Auto-updates

### System Requirements
- Windows 10/11 (64-bit recommended)
- .NET Framework 4.8
- Media Foundation (included in Windows; Media Feature Pack required for N editions)
- DirectX 11 compatible GPU (for hardware acceleration)

## Build Instructions

### Prerequisites
1. Visual Studio 2019 or later
2. .NET Framework 4.8 SDK

### Building the Project

```bash
# Clone the repository
git clone <repository-url>
cd screen-video-recorder

# Restore NuGet packages
nuget restore ScreenRecorder.sln

# Build with MSBuild
msbuild ScreenRecorder.sln /p:Configuration=Release /p:Platform="Any CPU"

# Or open in Visual Studio and build (Ctrl+Shift+B)
```

### Running the Application

```bash
# From Visual Studio: Press F5

# From command line:
cd src/ScreenRecorder/bin/Release
ScreenRecorder.exe
```

## Configuration

### Settings Location
- **Normal Mode**: `%APPDATA%\ScreenRecorder\settings.json`
- **Portable Mode**: `settings.json` (next to executable)

### Default Hotkeys
- **F9** - Start/Stop recording
- **F10** - Pause/Resume
- **F11** - Toggle microphone
- **F12** - Drop marker

### Output Location
Default: `%USERPROFILE%\Videos\Recordings\{yyyy-MM-dd}\clip_{HHmmss}.mp4`

Configurable via Settings with template variables:
- `{timestamp}` - Full timestamp (yyyyMMdd_HHmmss)
- `{date}` - Date only (yyyy-MM-dd)
- `{time}` - Time only (HH-mm-ss)
- `{profile}` - Profile name
- `{duration}` - Recording duration

## Usage Guide

### Quick Start
1. Launch ScreenRecorder
2. Select a recording profile
3. (Optional) Click "Select Region" to choose a specific area
4. Click "⏺ Start" or press F9
5. Record your content
6. Click "⏹ Stop" or press F9 again
7. Recording is automatically saved to your output folder

### Creating Custom Profiles
1. Click Settings (⚙)
2. Go to Profiles tab
3. Click "New Profile"
4. Configure:
   - Capture target (screen/window/region)
   - Video quality (FPS, bitrate, codec)
   - Audio sources and mixing
   - Auto-stop/split settings
5. Save and select the profile

### Recovery from Crashes
If the application crashes during recording:
1. Restart the application
2. Recordings are automatically recovered from temp files
3. Finalized recordings appear in your output folder

## Logging and Diagnostics

### Log Files
Location: `%APPDATA%\ScreenRecorder\logs\`

Files are rotated daily and retained for 30 days.

### Log Levels
- **Information** - Normal operation
- **Warning** - Recoverable issues
- **Error** - Failed operations
- **Debug** - Detailed trace (enable in settings)

### Diagnostics
Click "Copy Diagnostics" in Settings to get:
- OS version and architecture
- Available codecs
- Audio devices
- DPI scaling information
- Recent log entries

## Localization

Supported languages:
- English (en-US)
- Malay (ms-MY)

Translation files: `Resources/Strings/{locale}.resx`

## Troubleshooting

### Media Foundation Not Available
**Problem**: Error on startup about Media Foundation

**Solution**:
- Install the Media Feature Pack for Windows N editions
- Download from: https://support.microsoft.com/en-us/windows

### No Audio Recorded
**Problem**: Video has no audio

**Solutions**:
1. Check audio device selection in profile settings
2. Verify audio devices in Windows Sound settings
3. Ensure microphone permissions are granted
4. Check VU meters - if no movement, device may be muted

### Recording Stutters
**Problem**: Choppy video playback

**Solutions**:
1. Lower frame rate (60 → 30 FPS)
2. Reduce bitrate
3. Enable hardware acceleration
4. Close other resource-intensive applications
5. Record to SSD instead of HDD

### Hotkeys Not Working
**Problem**: Global hotkeys don't respond

**Solutions**:
1. Run as Administrator (some apps block hotkeys)
2. Change hotkey combination (may conflict with other software)
3. Check if another app has registered the same hotkey

## Development

### Code Style
- Follow C# naming conventions
- Use async/await for I/O operations
- MVVM pattern for UI code
- Dependency injection via interfaces

### Testing
```bash
# Unit tests (when implemented)
dotnet test
```

### Contributing
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is provided as-is for educational and commercial use.

## Acknowledgments

- **ScreenRecorderLib** - Core recording engine
- **NAudio** - Audio device management
- **Serilog** - Logging framework
- **SimpleInjector** - Dependency injection

## Roadmap

### Planned Features
- [ ] GIF export
- [ ] Video trimming and quick editing
- [ ] Upload to YouTube/SharePoint
- [ ] Webcam overlay
- [ ] Annotation tools during recording
- [ ] Multi-language UI (extensible)
- [ ] Cloud backup integration
- [ ] Performance metrics and analytics

## Support

For issues, feature requests, or questions:
- Create an issue in the repository
- Check existing issues for solutions
- Consult the log files for diagnostic information

---

**Version**: 1.0.0
**Last Updated**: 2025-01-09
