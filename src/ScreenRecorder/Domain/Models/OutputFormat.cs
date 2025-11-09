namespace ScreenRecorder.Domain.Models
{
    public class OutputFormat
    {
        public VideoCodec VideoCodec { get; set; }
        public AudioCodec AudioCodec { get; set; }
        public ContainerFormat Container { get; set; }
        public FrameRate FrameRate { get; set; }
        public BitrateMode BitrateMode { get; set; }
        public int VideoBitrateMbps { get; set; }
        public int AudioBitrateKbps { get; set; }
        public QualityPreset QualityPreset { get; set; }
        public bool HardwareAcceleration { get; set; }

        public OutputFormat()
        {
            VideoCodec = VideoCodec.H264;
            AudioCodec = AudioCodec.AAC;
            Container = ContainerFormat.MP4;
            FrameRate = FrameRate.Fps30;
            BitrateMode = BitrateMode.Auto;
            VideoBitrateMbps = 8;
            AudioBitrateKbps = 192;
            QualityPreset = QualityPreset.High;
            HardwareAcceleration = true;
        }

        public OutputFormat Clone()
        {
            return new OutputFormat
            {
                VideoCodec = VideoCodec,
                AudioCodec = AudioCodec,
                Container = Container,
                FrameRate = FrameRate,
                BitrateMode = BitrateMode,
                VideoBitrateMbps = VideoBitrateMbps,
                AudioBitrateKbps = AudioBitrateKbps,
                QualityPreset = QualityPreset,
                HardwareAcceleration = HardwareAcceleration
            };
        }
    }
}
