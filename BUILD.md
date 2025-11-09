# Build Instructions

## Prerequisites

1. **Windows 10/11** (required for .NET Framework 4.8)
2. **Visual Studio 2019 or later** with:
   - .NET desktop development workload
   - .NET Framework 4.8 SDK
3. **NuGet CLI** (or use Visual Studio's built-in NuGet)

## Step-by-Step Build Process

### Option 1: Using Visual Studio (Recommended)

1. **Open the Solution**
   ```
   Double-click: ScreenRecorder.sln
   ```

2. **Restore NuGet Packages**
   - Right-click on the solution in Solution Explorer
   - Select "Restore NuGet Packages"
   - Wait for all packages to download (may take 1-2 minutes)

3. **Build the Solution**
   - Press `Ctrl+Shift+B` or
   - Menu: Build → Build Solution

4. **Run the Application**
   - Press `F5` for debug mode
   - Press `Ctrl+F5` for release mode without debugging

### Option 2: Using Command Line

1. **Navigate to Project Directory**
   ```cmd
   cd /path/to/screen-video-recorder
   ```

2. **Restore NuGet Packages**
   ```cmd
   nuget restore ScreenRecorder.sln
   ```

   If `nuget` is not in PATH, download from: https://www.nuget.org/downloads

3. **Build with MSBuild**
   ```cmd
   msbuild ScreenRecorder.sln /p:Configuration=Release /p:Platform="Any CPU"
   ```

   Or for Debug build:
   ```cmd
   msbuild ScreenRecorder.sln /p:Configuration=Debug /p:Platform="Any CPU"
   ```

4. **Run the Application**
   ```cmd
   cd src\ScreenRecorder\bin\Release
   ScreenRecorder.exe
   ```

## Troubleshooting

### Error: "The type or namespace name 'X' could not be found"

**Cause**: NuGet packages haven't been restored.

**Solution**:
1. Delete the `packages` folder (if it exists)
2. Delete `bin` and `obj` folders in `src/ScreenRecorder/`
3. Restore packages again:
   ```cmd
   nuget restore ScreenRecorder.sln
   ```
4. Rebuild the solution

### Error: "Could not find version 'X' of package 'Y'"

**Cause**: Package version doesn't exist or NuGet cache is corrupted.

**Solution**:
1. Clear NuGet cache:
   ```cmd
   nuget locals all -clear
   ```
2. Restore packages again

### Error: "AForge.Video.FFMPEG" not found at runtime

**Cause**: FFMPEG native DLLs not copied to output directory.

**Solution**:
1. Check that `AForge.Video.FFMPEG.2.2.5` package is installed
2. Verify FFMPEG DLLs are in `bin\Debug` or `bin\Release`:
   - `avcodec-57.dll`
   - `avformat-57.dll`
   - `avutil-55.dll`
   - `swscale-4.dll`
3. If missing, manually copy from:
   ```
   packages\AForge.Video.FFMPEG.2.2.5\build\x86\
   ```

### Error: Platform mismatch (x86 vs x64)

**Cause**: AForge.Video.FFMPEG is x86, but project is set to x64.

**Solution**:
1. In Visual Studio: Configuration Manager
2. Set platform to: **Any CPU**
3. Uncheck: "Prefer 32-bit"
4. Rebuild

## Package Verification

After restoring packages, verify these folders exist:

```
packages/
├── SimpleInjector.5.4.1/
├── Serilog.3.1.1/
├── Serilog.Sinks.File.5.0.0/
├── Newtonsoft.Json.13.0.3/
├── NAudio.2.2.1/
├── NAudio.Core.2.2.1/
├── NAudio.Asio.2.2.1/
├── NAudio.Wasapi.2.2.1/
├── NAudio.WinMM.2.2.1/
├── AForge.2.2.5/
├── AForge.Video.2.2.5/
├── AForge.Video.FFMPEG.2.2.5/
└── Accord.Video.FFMPEG.x64.3.8.0/
```

## Build Output

Successful build will create:

```
src/ScreenRecorder/bin/Release/
├── ScreenRecorder.exe          # Main executable
├── ScreenRecorder.exe.config   # Configuration
├── SimpleInjector.dll
├── Serilog.dll
├── Serilog.Sinks.File.dll
├── Newtonsoft.Json.dll
├── NAudio.dll (and dependencies)
├── AForge.dll
├── AForge.Video.dll
├── AForge.Video.FFMPEG.dll
├── Accord.Video.FFMPEG.dll
└── FFMPEG DLLs (avcodec, avformat, etc.)
```

## First Run

When you first run the application:

1. It will create settings in: `%APPDATA%\ScreenRecorder\`
2. Log files will be written to: `%APPDATA%\ScreenRecorder\logs\`
3. Default recording location: `%USERPROFILE%\Videos\Recordings\`

## Development Build

For development with debugging:

1. Build in **Debug** configuration
2. Attach debugger (F5)
3. Check logs in: `%APPDATA%\ScreenRecorder\logs\log-{date}.txt`
4. Enable verbose logging by editing code:
   ```csharp
   // In LogService.cs, change:
   _currentLogLevel = LogEventLevel.Debug;
   ```

## Known Build Issues

### Issue 1: Missing .NET Framework 4.8

**Error**: "Project targets framework '.NETFramework,Version=v4.8' which is not installed"

**Solution**:
- Install .NET Framework 4.8 SDK
- Download from: https://dotnet.microsoft.com/download/dotnet-framework/net48

### Issue 2: Visual Studio can't find MSBuild

**Error**: "The command 'msbuild' is not recognized"

**Solution**:
Use Developer Command Prompt for VS:
- Start Menu → Visual Studio 2019 → Developer Command Prompt
- Or add to PATH:
  ```
  C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin
  ```

### Issue 3: NuGet.exe not found

**Solution**:
Download NuGet CLI:
```cmd
# Download
Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile nuget.exe

# Or visit: https://www.nuget.org/downloads
```

## Performance Build (Release)

For optimal performance:

1. Build in **Release** mode
2. Optimization is enabled automatically
3. Debug symbols are minimal (PDB only)
4. Assembly is signed (if configured)

## Testing the Build

After building, test basic functionality:

```cmd
# Run the application
ScreenRecorder.exe

# Test actions:
1. Select "Quick 1080p/30" profile
2. Click "Start Recording"
3. Record for 5 seconds
4. Click "Stop"
5. Verify video file created in: %USERPROFILE%\Videos\Recordings\
6. Open video file to verify it plays correctly
```

## Clean Build

To perform a complete clean build:

```cmd
# Delete all build artifacts
rmdir /s /q src\ScreenRecorder\bin
rmdir /s /q src\ScreenRecorder\obj
rmdir /s /q packages

# Restore and rebuild
nuget restore ScreenRecorder.sln
msbuild ScreenRecorder.sln /t:Rebuild /p:Configuration=Release
```

## Continuous Integration

For CI/CD pipelines:

```yaml
# Example GitHub Actions / Azure DevOps
steps:
  - task: NuGetToolInstaller@1

  - task: NuGetCommand@2
    inputs:
      command: 'restore'
      restoreSolution: 'ScreenRecorder.sln'

  - task: MSBuild@1
    inputs:
      solution: 'ScreenRecorder.sln'
      configuration: 'Release'
      platform: 'Any CPU'
```

## Support

If build issues persist:
1. Check `BUILD.log` in project root
2. Review error messages carefully
3. Verify all prerequisites are installed
4. Try clean build process
5. Check GitHub issues for similar problems

---

**Last Updated**: 2025-01-09
**Tested With**: Visual Studio 2019/2022, .NET Framework 4.8
