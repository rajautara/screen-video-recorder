using System;
using System.Collections.Generic;

namespace ScreenRecorder.Domain.Models
{
    public class RecordingSession
    {
        public string Id { get; set; }
        public RecordingProfile Profile { get; set; }
        public RecordingState State { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan PausedDuration { get; set; }
        public string TempFilePath { get; set; }
        public string FinalFilePath { get; set; }
        public long FileSizeBytes { get; set; }
        public List<TimeSpan> Markers { get; set; }
        public string ErrorMessage { get; set; }
        public int CurrentSegment { get; set; }
        public bool IsRecovery { get; set; }

        public RecordingSession()
        {
            Id = Guid.NewGuid().ToString();
            State = RecordingState.Idle;
            Markers = new List<TimeSpan>();
            CurrentSegment = 1;
        }

        public TimeSpan GetEffectiveDuration()
        {
            return Duration - PausedDuration;
        }

        public string GetFormattedDuration()
        {
            var effective = GetEffectiveDuration();
            if (effective.TotalHours >= 1)
                return effective.ToString(@"hh\:mm\:ss");
            else
                return effective.ToString(@"mm\:ss");
        }

        public void AddMarker(TimeSpan position)
        {
            if (!Markers.Contains(position))
            {
                Markers.Add(position);
                Markers.Sort();
            }
        }
    }
}
