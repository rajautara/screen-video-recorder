using System.IO;

namespace ScreenRecorder.Infrastructure.Services
{
    /// <summary>
    /// Service for file operations
    /// </summary>
    public class FileService : IFileService
    {
        public void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            File.Copy(sourceFileName, destFileName, overwrite);
        }

        public void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public void Move(string sourceFileName, string destFileName)
        {
            File.Move(sourceFileName, destFileName);
        }

        public long GetFileSize(string path)
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                return fileInfo.Length;
            }
            return 0;
        }
    }
}
