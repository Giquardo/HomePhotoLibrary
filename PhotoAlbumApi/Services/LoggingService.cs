using Microsoft.Extensions.Logging;
using Serilog;

namespace PhotoAlbumApi.Services
{
    public interface ILoggingService
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(Exception exception, string message);
        void LogDebug(string message);
    }

    public class LoggingService : ILoggingService
    {
        private readonly ILogger<LoggingService> _serilogLogger;

        public LoggingService()
        {
            // Route through the existing Serilog pipeline (console + the
            // retention-capped rolling file sink) instead of also writing a
            // second, uncapped copy of every message to its own file.
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            });

            _serilogLogger = loggerFactory.CreateLogger<LoggingService>();
        }

        public void LogInformation(string message) => _serilogLogger.LogInformation(message);

        public void LogWarning(string message) => _serilogLogger.LogWarning(message);

        public void LogError(string message) => _serilogLogger.LogError(message);

        public void LogError(Exception exception, string message) => _serilogLogger.LogError(exception, message);

        public void LogDebug(string message) => _serilogLogger.LogDebug(message);
    }
}
