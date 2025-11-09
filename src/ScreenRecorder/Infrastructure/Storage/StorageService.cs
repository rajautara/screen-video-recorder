using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ScreenRecorder.Domain.Models;
using ScreenRecorder.Infrastructure.Settings;
using Serilog;

namespace ScreenRecorder.Infrastructure.Storage
{
    public class StorageService : IStorageService
    {
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly string _tempDirectory;
        private readonly string _recoveryDirectory;

        public StorageService(ILogger logger, ISettingsService settingsService)
        {
            _logger = logger;
            _settingsService = settingsService;

            _tempDirectory = Path.Combine(Path.GetTempPath(), "ScreenRecorder", "temp");
            _recoveryDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ScreenRecorder",
                "recovery");

            EnsureDirectoryExists(_tempDirectory);
            EnsureDirectoryExists(_recoveryDirectory);
        }

        public string GetTempFilePath(RecordingSession session)
        {
            var fileName = $"recording_{session.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.tmp";
            return Path.Combine(_tempDirectory, fileName);
        }

        public string GetFinalFilePath(RecordingSession session, string extension)
        {
            var outputDir = GetOutputDirectory();

            if (_settingsService.Current.Storage.OrganizeByDate)
            {
                outputDir = Path.Combine(outputDir, DateTime.Now.ToString("yyyy-MM-dd"));
            }

            EnsureDirectoryExists(outputDir);

            var template = _settingsService.Current.Storage.FilenameTemplate;
            var fileName = GenerateFileName(template, session);
            fileName = $"{fileName}.{extension}";

            // Ensure unique filename
            var finalPath = Path.Combine(outputDir, fileName);
            int counter = 1;
            while (File.Exists(finalPath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                finalPath = Path.Combine(outputDir, $"{nameWithoutExt}_{counter}.{extension}");
                counter++;
            }

            return finalPath;
        }

        public bool FinalizeRecording(RecordingSession session)
        {
            try
            {
                if (!File.Exists(session.TempFilePath))
                {
                    _logger.Error("Temp file not found: {Path}", session.TempFilePath);
                    return false;
                }

                var extension = session.Profile.OutputFormat.Container.ToString().ToLower();
                var finalPath = GetFinalFilePath(session, extension);

                _logger.Information("Finalizing recording: {Temp} -> {Final}", session.TempFilePath, finalPath);

                // Try to move/copy the file
                File.Move(session.TempFilePath, finalPath);

                session.FinalFilePath = finalPath;
                session.FileSizeBytes = new FileInfo(finalPath).Length;

                _logger.Information("Recording finalized successfully: {Path} ({Size} bytes)",
                    finalPath, session.FileSizeBytes);

                // Clean up recovery info
                ClearRecoverySession(session.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to finalize recording");
                return false;
            }
        }

        public void SaveRecoveryInfo(RecordingSession session)
        {
            try
            {
                var recoveryPath = Path.Combine(_recoveryDirectory, $"{session.Id}.json");
                var json = JsonConvert.SerializeObject(session, Formatting.Indented);
                File.WriteAllText(recoveryPath, json);
                _logger.Debug("Recovery info saved for session {Id}", session.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save recovery info");
            }
        }

        public List<RecordingSession> GetRecoverySessions()
        {
            var sessions = new List<RecordingSession>();

            try
            {
                var files = Directory.GetFiles(_recoveryDirectory, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var session = JsonConvert.DeserializeObject<RecordingSession>(json);
                        if (session != null && File.Exists(session.TempFilePath))
                        {
                            sessions.Add(session);
                        }
                        else
                        {
                            // Clean up stale recovery file
                            File.Delete(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to load recovery session from {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get recovery sessions");
            }

            return sessions;
        }

        public void ClearRecoverySession(string sessionId)
        {
            try
            {
                var recoveryPath = Path.Combine(_recoveryDirectory, $"{sessionId}.json");
                if (File.Exists(recoveryPath))
                {
                    File.Delete(recoveryPath);
                    _logger.Debug("Recovery session cleared: {Id}", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to clear recovery session");
            }
        }

        public void RecoverFromCrash()
        {
            var recoverySessions = GetRecoverySessions();

            if (recoverySessions.Any())
            {
                _logger.Information("Found {Count} recovery session(s)", recoverySessions.Count);

                foreach (var session in recoverySessions)
                {
                    try
                    {
                        session.IsRecovery = true;
                        if (FinalizeRecording(session))
                        {
                            _logger.Information("Recovered session {Id}: {Path}",
                                session.Id, session.FinalFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failed to recover session {Id}", session.Id);
                    }
                }
            }
        }

        public string GetOutputDirectory()
        {
            return _settingsService.Current.Storage.OutputDirectory;
        }

        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.Debug("Created directory: {Path}", path);
            }
        }

        public long GetFileSize(string path)
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.Debug("Deleted file: {Path}", path);
            }
        }

        public string GenerateFileName(string template, RecordingSession session)
        {
            var fileName = template
                .Replace("{timestamp}", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
                .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"))
                .Replace("{time}", DateTime.Now.ToString("HH-mm-ss"))
                .Replace("{profile}", session.Profile?.Name ?? "recording")
                .Replace("{duration}", session.GetFormattedDuration().Replace(":", "-"));

            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }
    }
}
