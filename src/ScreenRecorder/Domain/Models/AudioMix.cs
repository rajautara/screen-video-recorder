namespace ScreenRecorder.Domain.Models
{
    public class AudioMix
    {
        public AudioSource Source { get; set; }
        public string SystemAudioDeviceId { get; set; }
        public string MicrophoneDeviceId { get; set; }
        public float SystemAudioVolume { get; set; }
        public float MicrophoneVolume { get; set; }
        public bool EnableNoiseSuppression { get; set; }
        public bool EnableEchoCancellation { get; set; }

        public AudioMix()
        {
            Source = AudioSource.Both;
            SystemAudioVolume = 1.0f;
            MicrophoneVolume = 1.0f;
            EnableNoiseSuppression = false;
            EnableEchoCancellation = false;
        }

        public AudioMix Clone()
        {
            return new AudioMix
            {
                Source = Source,
                SystemAudioDeviceId = SystemAudioDeviceId,
                MicrophoneDeviceId = MicrophoneDeviceId,
                SystemAudioVolume = SystemAudioVolume,
                MicrophoneVolume = MicrophoneVolume,
                EnableNoiseSuppression = EnableNoiseSuppression,
                EnableEchoCancellation = EnableEchoCancellation
            };
        }
    }
}
