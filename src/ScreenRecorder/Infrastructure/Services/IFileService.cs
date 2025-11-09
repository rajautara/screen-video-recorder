namespace ScreenRecorder.Infrastructure.Services
{
    /// <summary>
    /// Service for file operations
    /// </summary>
    public interface IFileService
    {
        void Copy(string sourceFileName, string destFileName, bool overwrite);
        void Delete(string path);
        bool Exists(string path);
        void Move(string sourceFileName, string destFileName);
        long GetFileSize(string path);
    }
}
