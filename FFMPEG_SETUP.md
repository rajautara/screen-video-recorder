# FFMPEG Setup for Screen Recorder

## Issue
The application uses AForge.Video.FFMPEG library which requires native FFMPEG DLLs to function properly. Without these DLLs, you'll see errors like:

```
Could not load file or assembly 'AForge.Video.FFMPEG, Version=2.2.5.0, Culture=neutral, PublicKeyToken=03563089b1be05dd' or one of its dependencies.
```

## Solution Options

### Option 1: Install FFMPEG Native DLLs (Quick Fix)

1. Download the AForge.Video.FFMPEG native binaries
2. Copy the following DLLs to the application's bin\Debug folder:
   - `avcodec-55.dll`
   - `avformat-55.dll`
   - `avutil-52.dll`
   - `swscale-2.dll`

These DLLs can be found in the AForge.Video.FFMPEG NuGet package or downloaded from the AForge.NET website.

### Option 2: Use a Different Video Library (Recommended for Production)

Consider replacing AForge.Video.FFMPEG with a more modern alternative:

- **FFMediaToolkit** - Modern .NET wrapper for FFMPEG
- **MediaToolkit** - Another FFMPEG wrapper
- **SharpAvi** - Pure .NET AVI writer (no FFMPEG needed)
- **Accord.Video.FFMPEG** - More actively maintained than AForge

### Option 3: Platform-Specific Build

Set the project's platform target to x86 or x64 (not AnyCPU) to match the FFMPEG binaries architecture:

1. Right-click on the project
2. Properties → Build
3. Set Platform target to x64 (recommended for modern systems)
4. Rebuild the solution

## Current Status

The application will now show a more helpful error message when FFMPEG is not available:

```
Could not initialize video recording. This application requires FFMPEG libraries.
Please ensure AForge.Video.FFMPEG and its dependencies are properly installed.
```

## For Developers

To properly deploy this application, you need to:

1. Include the native FFMPEG DLLs in your deployment package
2. Or switch to a library that includes the native binaries
3. Or use Windows Media Foundation API instead of FFMPEG
