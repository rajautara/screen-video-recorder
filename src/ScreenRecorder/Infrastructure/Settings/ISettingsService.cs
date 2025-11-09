using ScreenRecorder.Domain.Models;
using System;

namespace ScreenRecorder.Infrastructure.Settings
{
    public interface ISettingsService
    {
        AppSettings Load();
        void Save(AppSettings settings);
        AppSettings Current { get; }
        event EventHandler<AppSettings> SettingsChanged;
        void ExportSettings(string filePath);
        AppSettings ImportSettings(string filePath);
        string GetSettingsFilePath();
    }
}
