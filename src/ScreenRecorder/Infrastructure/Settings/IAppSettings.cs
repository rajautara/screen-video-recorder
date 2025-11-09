using ScreenRecorder.Domain.Models;
using System.Drawing;

namespace ScreenRecorder.Infrastructure.Settings
{
    /// <summary>
    /// Provides access to application settings with simplified property access
    /// </summary>
    public interface IAppSettings
    {
        string OutputDirectory { get; set; }
        VideoCodec VideoCodec { get; set; }
        AudioCodec AudioCodec { get; set; }
        int FrameRate { get; set; }
        string AudioDeviceId { get; set; }
        bool IsSystemAudioEnabled { get; set; }
        bool IsMicrophoneEnabled { get; set; }
        Rectangle LastSelectedRegion { get; set; }
        
        void Save();
        void Load();
    }
}
