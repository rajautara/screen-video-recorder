using System;
using System.Collections.Generic;

namespace ScreenRecorder.Infrastructure.Audio
{
    public class AudioDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public AudioDeviceType Type { get; set; }
    }

    public enum AudioDeviceType
    {
        Output,
        Input
    }

    public interface IAudioDeviceService
    {
        List<AudioDevice> GetOutputDevices();
        List<AudioDevice> GetInputDevices();
        AudioDevice GetDefaultOutputDevice();
        AudioDevice GetDefaultInputDevice();
        event EventHandler DeviceListChanged;
        float GetDeviceLevel(string deviceId);
    }
}
