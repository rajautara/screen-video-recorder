using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace ScreenRecorder.Infrastructure.Logging
{
    public class LogService : ILogService
    {
        private readonly string _logDirectory;
        private LogEventLevel _currentLogLevel;

        public LogService()
        {
            _logDirectory = GetLogDirectory();
            _currentLogLevel = LogEventLevel.Information;
            Directory.CreateDirectory(_logDirectory);
        }

        public ILogger CreateLogger()
        {
            var logPath = Path.Combine(_logDirectory, "log-.txt");

            var logger = new LoggerConfiguration()
                .MinimumLevel.Is(_currentLogLevel)
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Logger = logger;

            logger.Information("=== ScreenRecorder Started ===");
            logger.Information("OS Version: {OSVersion}", Environment.OSVersion);
            logger.Information("CLR Version: {CLRVersion}", Environment.Version);
            logger.Information("64-bit OS: {Is64Bit}", Environment.Is64BitOperatingSystem);
            logger.Information("64-bit Process: {Is64BitProcess}", Environment.Is64BitProcess);
            logger.Information("Log Directory: {LogDirectory}", _logDirectory);

            return logger;
        }

        public void SetLogLevel(LogEventLevel level)
        {
            _currentLogLevel = level;
        }

        public string GetLogDirectory()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "ScreenRecorder", "logs");
        }
    }
}
