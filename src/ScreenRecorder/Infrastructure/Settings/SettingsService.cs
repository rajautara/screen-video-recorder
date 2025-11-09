using System;
using System.IO;
using Newtonsoft.Json;
using ScreenRecorder.Domain.Models;
using Serilog;

namespace ScreenRecorder.Infrastructure.Settings
{
    public class SettingsService : ISettingsService
    {
        private readonly ILogger _logger;
        private readonly string _settingsPath;
        private AppSettings _currentSettings;

        public AppSettings Current => _currentSettings;
        public event EventHandler<AppSettings> SettingsChanged;

        public SettingsService(ILogger logger)
        {
            _logger = logger;
            _settingsPath = GetSettingsFilePath();
            _currentSettings = Load();
        }

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    _logger.Information("Settings loaded from {Path}", _settingsPath);
                    return settings ?? new AppSettings();
                }
                else
                {
                    _logger.Information("No settings file found, creating default settings");
                    var defaultSettings = new AppSettings();
                    Save(defaultSettings);
                    return defaultSettings;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load settings, using defaults");
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            try
            {
                settings.ModifiedDate = DateTime.Now;
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_settingsPath, json);

                _currentSettings = settings;
                _logger.Information("Settings saved to {Path}", _settingsPath);
                SettingsChanged?.Invoke(this, settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save settings");
                throw;
            }
        }

        public void ExportSettings(string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(_currentSettings, Formatting.Indented);
                File.WriteAllText(filePath, json);
                _logger.Information("Settings exported to {Path}", filePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to export settings");
                throw;
            }
        }

        public AppSettings ImportSettings(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                _logger.Information("Settings imported from {Path}", filePath);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to import settings");
                throw;
            }
        }

        public string GetSettingsFilePath()
        {
            // Check for portable mode
            var portableSettingsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "settings.json");

            if (File.Exists(portableSettingsPath))
            {
                return portableSettingsPath;
            }

            // Use AppData
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "ScreenRecorder", "settings.json");
        }
    }
}
