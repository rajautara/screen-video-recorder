using Serilog;

namespace ScreenRecorder.Infrastructure.Logging
{
    public interface ILogService
    {
        ILogger CreateLogger();
        void SetLogLevel(Serilog.Events.LogEventLevel level);
        string GetLogDirectory();
    }
}
