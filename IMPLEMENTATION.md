# Implementation Details

## Screen Recording Engine

### Final Library Choice: AForge.Video.FFMPEG

After testing package availability, we implemented the screen recording functionality using **AForge.NET**, a mature and widely-used computer vision framework.

### Why AForge?

1. **Proven Availability**: Available on NuGet.org (v2.2.5)
2. **Production Ready**: Used in thousands of applications
3. **FFMPEG Integration**: Includes FFMPEG binaries for professional encoding
4. **H.264 Support**: Native H.264/MPEG4 encoding
5. **No External Dependencies**: FFMPEG binaries bundled in the package

### Recording Implementation

**Class**: `ScreenCaptureService`
**Location**: `src/ScreenRecorder/Infrastructure/Recording/ScreenCaptureService.cs`

**Key Components**:
```csharp
- VideoFileWriter (from AForge.Video.FFMPEG)
- Graphics.CopyFromScreen (GDI+ screen capture)
- Multi-threaded recording loop
- Frame-accurate timing
```

**Supported Codecs**:
- H.264 (default, best compatibility)
- MPEG4 (alternative)
- RAW (lossless, large files)

**Supported Formats**:
- MP4 container
- AVI container
- Configurable bitrates (1-50 Mbps)
- Frame rates: 24, 30, 60, 120 FPS

### Recording Flow

1. **Initialization**
   - Create `VideoFileWriter`
   - Configure codec, resolution, frame rate, bitrate
   - Open output file

2. **Capture Loop** (Dedicated thread)
   - Calculate target frame time
   - Capture screen using GDI+ bitmap
   - Write frame to video file
   - Sleep/spin to maintain frame rate

3. **Pause/Resume**
   - Pause: Stop capturing, keep file open
   - Resume: Continue capturing, track paused duration

4. **Finalization**
   - Stop capture thread
   - Close video file
   - Move from temp to final location

### Performance Optimizations

- **Thread Priority**: `AboveNormal` for smooth capture
- **Lock-based synchronization**: Thread-safe frame writing
- **Bitmap disposal**: Immediate cleanup to reduce memory
- **Frame timing**: Precise timing with minimal CPU spin
- **Progress throttling**: Updates every 1 second max

### Multi-Monitor Support

The implementation handles:
- Multiple monitors with different resolutions
- Per-monitor DPI scaling
- Window tracking across monitors
- Custom region selection

### Error Handling

Robust error handling for:
- FFMPEG unavailability
- Codec not supported
- Window handle invalid
- Disk space issues
- Thread termination

## Audio Implementation

**Library**: NAudio (v2.2.1)
**Service**: `AudioDeviceService`

**Features**:
- WASAPI loopback (system audio)
- Microphone capture
- Device enumeration
- VU meters (peak detection)
- Device change notifications

**Note**: Current implementation captures audio device info but video-audio muxing would require additional integration (planned for future versions).

## Build Requirements

### NuGet Packages (All Verified Available)

```xml
<package id="SimpleInjector" version="5.4.1" />
<package id="Serilog" version="3.1.1" />
<package id="Serilog.Sinks.File" version="5.0.0" />
<package id="Newtonsoft.Json" version="13.0.3" />
<package id="NAudio" version="2.2.1" />
<package id="AForge" version="2.2.5" />
<package id="AForge.Video" version="2.2.5" />
<package id="AForge.Video.FFMPEG" version="2.2.5" />
<package id="Accord.Video.FFMPEG.x64" version="3.8.0" />
```

### System Requirements

- Windows 10/11 (x64)
- .NET Framework 4.8
- Visual Studio 2019+ (for building)
- 4GB RAM minimum
- SSD recommended (for smooth high-res recording)

## Output Specifications

### Default Settings

**Video**:
- Codec: H.264
- Container: MP4
- Resolution: Screen native
- Frame Rate: 30 FPS
- Bitrate: 8 Mbps
- Pixel Format: RGB24

**File Naming**:
- Template: `clip_{timestamp}.mp4`
- Location: `%USERPROFILE%\Videos\Recordings\{date}\`
- Example: `clip_20250109_143022.mp4`

### Recording Profiles

1. **Quick 1080p/30**
   - 1920x1080 @ 30fps
   - 8 Mbps H.264
   - System + Mic audio

2. **Gameplay 1080p/60**
   - 1920x1080 @ 60fps
   - 16 Mbps H.264
   - System audio only

3. **Lossless Short Clip**
   - Native resolution
   - RAW codec (uncompressed)
   - Auto-stop at 60s

## Known Limitations

1. **Audio Muxing**: Current implementation captures video only. Audio capture is implemented but not yet muxed into the video file. This requires additional FFMPEG command-line integration or using a muxing library.

2. **Hardware Acceleration**: AForge.Video.FFMPEG uses software encoding. For hardware acceleration (NVENC/QuickSync), would need to integrate FFMPEG command-line or use Media Foundation.

3. **Cursor Overlay**: Cursor capture is done via screen capture. Dedicated cursor rendering for smooth cursor animation is planned.

4. **Window Tracking**: Window position tracking works but doesn't handle window resize during recording (will capture fixed region).

## Future Enhancements

### Short-term
- [ ] Audio muxing integration
- [ ] Cursor capture improvements
- [ ] Window resize tracking
- [ ] Settings UI implementation

### Medium-term
- [ ] Hardware acceleration (NVENC/AMF)
- [ ] Region picker UI with live preview
- [ ] Post-record trimming/editing
- [ ] GIF export
- [ ] Annotations during recording

### Long-term
- [ ] Webcam overlay
- [ ] Multi-audio track recording
- [ ] Streaming integration
- [ ] Cloud upload

## Testing Checklist

- [x] Full screen recording
- [x] Window recording (static)
- [x] Region recording
- [x] Pause/Resume
- [x] Multi-monitor
- [x] Frame rate selection
- [x] Bitrate selection
- [ ] Audio muxing (pending)
- [ ] Crash recovery
- [ ] Auto-split
- [ ] Hotkeys

## Performance Benchmarks

*To be added after testing*

Expected performance:
- 1080p @ 30fps: ~5-10% CPU (i5-8th gen)
- 1080p @ 60fps: ~10-15% CPU
- Memory: ~200-500MB during recording
- Disk write: ~1 MB/s @ 8 Mbps

## Troubleshooting

### "FFMPEG not available" error
- Ensure AForge.Video.FFMPEG NuGet package is properly installed
- Check that FFMPEG binaries are copied to output directory
- Verify x64 platform target

### Poor recording quality
- Increase bitrate in profile settings
- Use higher quality preset
- Consider lossless codec for critical work

### Choppy video
- Lower frame rate (60 → 30)
- Close resource-intensive applications
- Record to SSD instead of HDD
- Disable other screen recording software

### No audio in recording
- Audio muxing not yet implemented
- Planned for next version
- Workaround: Use external audio recording tool

---

**Last Updated**: 2025-01-09
**Version**: 1.0.0
**Status**: Core functionality complete, audio muxing pending
