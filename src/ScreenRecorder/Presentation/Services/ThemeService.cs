using System;
using System.Linq;
using System.Windows;
using ScreenRecorder.Domain.Models;
using Serilog;

namespace ScreenRecorder.Presentation.Services
{
    public class ThemeService : IThemeService
    {
        private readonly ILogger _logger;
        private ThemeMode _currentTheme;

        public ThemeMode CurrentTheme => _currentTheme;

        public ThemeService(ILogger logger)
        {
            _logger = logger;
            _currentTheme = ThemeMode.Light;
        }

        public void ApplyTheme(ThemeMode theme)
        {
            try
            {
                _currentTheme = theme;

                var app = Application.Current;
                if (app == null)
                    return;

                // Clear existing theme dictionaries
                var existingThemes = app.Resources.MergedDictionaries
                    .Where(d => d.Source != null &&
                        (d.Source.OriginalString.Contains("LightTheme") ||
                         d.Source.OriginalString.Contains("DarkTheme") ||
                         d.Source.OriginalString.Contains("HighContrast")))
                    .ToList();

                foreach (var dict in existingThemes)
                {
                    app.Resources.MergedDictionaries.Remove(dict);
                }

                // Add new theme dictionary
                var themeUri = theme switch
                {
                    ThemeMode.Dark => "Resources/Themes/DarkTheme.xaml",
                    ThemeMode.HighContrast => "Resources/Themes/HighContrastTheme.xaml",
                    _ => "Resources/Themes/LightTheme.xaml"
                };

                var themeDict = new ResourceDictionary
                {
                    Source = new Uri(themeUri, UriKind.Relative)
                };

                app.Resources.MergedDictionaries.Insert(0, themeDict);

                _logger.Information("Theme applied: {Theme}", theme);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to apply theme: {Theme}", theme);
            }
        }
    }
}
