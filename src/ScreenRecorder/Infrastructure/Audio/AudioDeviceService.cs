using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Serilog;

namespace ScreenRecorder.Infrastructure.Audio
{
    public class AudioDeviceService : IAudioDeviceService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly MMNotificationClient _notificationClient;

        public event EventHandler DeviceListChanged;

        public AudioDeviceService(ILogger logger)
        {
            _logger = logger;
            _deviceEnumerator = new MMDeviceEnumerator();
            _notificationClient = new MMNotificationClient();
            _notificationClient.DeviceAdded += OnDeviceChanged;
            _notificationClient.DeviceRemoved += OnDeviceChanged;
            _notificationClient.DeviceStateChanged += OnDeviceChanged;
            _notificationClient.DefaultDeviceChanged += OnDefaultDeviceChanged;
            _deviceEnumerator.RegisterEndpointNotificationCallback(_notificationClient);

            _logger.Information("AudioDeviceService initialized");
        }

        public List<AudioDevice> GetOutputDevices()
        {
            var devices = new List<AudioDevice>();

            try
            {
                var mmDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                foreach (var device in mmDevices)
                {
                    devices.Add(new AudioDevice
                    {
                        Id = device.ID,
                        Name = device.FriendlyName,
                        IsDefault = device.ID == defaultDevice.ID,
                        Type = AudioDeviceType.Output
                    });
                }

                _logger.Debug("Found {Count} output devices", devices.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to enumerate output devices");
            }

            return devices;
        }

        public List<AudioDevice> GetInputDevices()
        {
            var devices = new List<AudioDevice>();

            try
            {
                var mmDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);

                foreach (var device in mmDevices)
                {
                    devices.Add(new AudioDevice
                    {
                        Id = device.ID,
                        Name = device.FriendlyName,
                        IsDefault = device.ID == defaultDevice.ID,
                        Type = AudioDeviceType.Input
                    });
                }

                _logger.Debug("Found {Count} input devices", devices.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to enumerate input devices");
            }

            return devices;
        }

        public AudioDevice GetDefaultOutputDevice()
        {
            try
            {
                var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                return new AudioDevice
                {
                    Id = device.ID,
                    Name = device.FriendlyName,
                    IsDefault = true,
                    Type = AudioDeviceType.Output
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get default output device");
                return null;
            }
        }

        public AudioDevice GetDefaultInputDevice()
        {
            try
            {
                var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                return new AudioDevice
                {
                    Id = device.ID,
                    Name = device.FriendlyName,
                    IsDefault = true,
                    Type = AudioDeviceType.Input
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get default input device");
                return null;
            }
        }

        public float GetDeviceLevel(string deviceId)
        {
            try
            {
                var devices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
                var device = devices.FirstOrDefault(d => d.ID == deviceId);

                if (device != null)
                {
                    return device.AudioMeterInformation.MasterPeakValue;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get device level for {DeviceId}", deviceId);
            }

            return 0f;
        }

        private void OnDeviceChanged(object sender, DeviceNotificationEventArgs e)
        {
            _logger.Debug("Audio device list changed");
            DeviceListChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnDefaultDeviceChanged(object sender, DefaultDeviceChangedEventArgs e)
        {
            _logger.Information("Default audio device changed: {Role} -> {DeviceId}", e.Role, e.DeviceId);
            DeviceListChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            try
            {
                if (_deviceEnumerator != null)
                {
                    _deviceEnumerator.UnregisterEndpointNotificationCallback(_notificationClient);
                    _deviceEnumerator.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error disposing AudioDeviceService");
            }
        }
    }

    // MMNotificationClient implementation for device change notifications
    internal class MMNotificationClient : IMMNotificationClient
    {
        public event EventHandler<DeviceNotificationEventArgs> DeviceAdded;
        public event EventHandler<DeviceNotificationEventArgs> DeviceRemoved;
        public event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
        public event EventHandler<DefaultDeviceChangedEventArgs> DefaultDeviceChanged;

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs { DeviceId = deviceId, State = newState });
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            DeviceAdded?.Invoke(this, new DeviceNotificationEventArgs { DeviceId = pwstrDeviceId });
        }

        public void OnDeviceRemoved(string deviceId)
        {
            DeviceRemoved?.Invoke(this, new DeviceNotificationEventArgs { DeviceId = deviceId });
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            DefaultDeviceChanged?.Invoke(this, new DefaultDeviceChangedEventArgs
            {
                DataFlow = flow,
                Role = role,
                DeviceId = defaultDeviceId
            });
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // Not implemented
        }
    }

    internal class DeviceNotificationEventArgs : EventArgs
    {
        public string DeviceId { get; set; }
    }

    internal class DeviceStateChangedEventArgs : EventArgs
    {
        public string DeviceId { get; set; }
        public DeviceState State { get; set; }
    }

    internal class DefaultDeviceChangedEventArgs : EventArgs
    {
        public DataFlow DataFlow { get; set; }
        public Role Role { get; set; }
        public string DeviceId { get; set; }
    }
}
