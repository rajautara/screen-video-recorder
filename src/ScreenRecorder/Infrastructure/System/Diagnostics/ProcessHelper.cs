using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ScreenRecorder.Infrastructure.System.Diagnostics
{
    public static class ProcessHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        /// <summary>
        /// Brings an existing process window to the foreground
        /// </summary>
        /// <param name="process">The process to bring to the foreground</param>
        public static void BringToForeground(Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            try
            {
                // Check if the process has a main window
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    // Restore the window if it's minimized
                    ShowWindow(process.MainWindowHandle, SW_RESTORE);
                    
                    // Bring the window to the foreground
                    SetForegroundWindow(process.MainWindowHandle);
                }
            }
            catch (Exception ex)
            {
                // Log the error if logging is available
                // _logger?.Error(ex, "Failed to bring process to foreground");
                throw new InvalidOperationException("Failed to bring process to foreground", ex);
            }
        }

        /// <summary>
        /// Checks if a process with the given name is already running
        /// </summary>
        /// <param name="processName">Name of the process (without .exe)</param>
        /// <returns>True if the process is running, false otherwise</returns>
        public static bool IsProcessRunning(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                throw new ArgumentException("Process name cannot be null or empty", nameof(processName));

            // Remove .exe if present
            processName = Path.GetFileNameWithoutExtension(processName);
            
            return Process.GetProcessesByName(processName).Length > 0;
        }

        /// <summary>
        /// Kills a process by its name
        /// </summary>
        /// <param name="processName">Name of the process to kill (without .exe)</param>
        public static void KillProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                throw new ArgumentException("Process name cannot be null or empty", nameof(processName));

            // Remove .exe if present
            processName = Path.GetFileNameWithoutExtension(processName);
            
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(5000); // Wait up to 5 seconds for the process to exit
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is global::System.ComponentModel.Win32Exception)
                {
                    // Log the error if logging is available
                    // _logger?.Error(ex, $"Failed to kill process: {process.ProcessName} (ID: {process.Id})");
                    throw new InvalidOperationException($"Failed to kill process: {process.ProcessName}", ex);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        /// <summary>
        /// Starts a process with the given file path and arguments
        /// </summary>
        /// <param name="fileName">Path to the executable file</param>
        /// <param name="arguments">Command-line arguments</param>
        /// <param name="workingDirectory">Working directory (optional)</param>
        /// <param name="waitForExit">Whether to wait for the process to exit</param>
        /// <returns>The started process, or null if waitForExit is true and the process completed</returns>
        public static Process StartProcess(string fileName, string arguments = null, string workingDirectory = null, bool waitForExit = false)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments ?? string.Empty,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                if (!string.IsNullOrEmpty(workingDirectory))
                {
                    startInfo.WorkingDirectory = workingDirectory;
                }

                var process = new Process { StartInfo = startInfo };
                process.Start();

                if (waitForExit)
                {
                    process.WaitForExit();
                    process.Dispose();
                    return null;
                }

                return process;
            }
            catch (Exception ex)
            {
                // Log the error if logging is available
                // _logger?.Error(ex, $"Failed to start process: {fileName}");
                throw new InvalidOperationException($"Failed to start process: {fileName}", ex);
            }
        }
    }
}
