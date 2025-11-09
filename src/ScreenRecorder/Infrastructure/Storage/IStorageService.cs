using ScreenRecorder.Domain.Models;
using System.Collections.Generic;

namespace ScreenRecorder.Infrastructure.Storage
{
    public interface IStorageService
    {
        string GetTempFilePath(RecordingSession session);
        string GetFinalFilePath(RecordingSession session, string extension);
        bool FinalizeRecording(RecordingSession session);
        void SaveRecoveryInfo(RecordingSession session);
        List<RecordingSession> GetRecoverySessions();
        void ClearRecoverySession(string sessionId);
        void RecoverFromCrash();
        string GetOutputDirectory();
        void EnsureDirectoryExists(string path);
        long GetFileSize(string path);
        bool FileExists(string path);
        void DeleteFile(string path);
        string GenerateFileName(string template, RecordingSession session);
    }
}
