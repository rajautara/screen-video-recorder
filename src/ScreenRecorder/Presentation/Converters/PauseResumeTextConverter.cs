using System;
using System.Globalization;
using System.Windows.Data;

namespace ScreenRecorder.Presentation.Converters
{
    /// <summary>
    /// Converts a boolean IsPaused value to "Pause" or "Resume" text
    /// </summary>
    public class PauseResumeTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPaused)
            {
                return isPaused ? "Resume" : "Pause";
            }
            
            return "Pause";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
