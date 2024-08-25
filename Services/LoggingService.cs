using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;

public class LoggingService
{
    private readonly string _filePath;
    private readonly ILogger<LoggingService> _serilogLogger;

    public LoggingService(string filePath)
    {
        _filePath = filePath;
        EnsureLogFileExists();

        // Use the existing Serilog configuration
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
        });

        _serilogLogger = loggerFactory.CreateLogger<LoggingService>();
    }

    private void EnsureLogFileExists()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(_filePath))
        {
            File.Create(_filePath).Dispose();
        }
    }

    public void LogInformation(string message)
    {
        Log("Information", message);
        _serilogLogger.LogInformation(message);
    }

    public void LogWarning(string message)
    {
        Log("Warning", message);
        _serilogLogger.LogWarning(message);
    }

    public void LogError(string message)
    {
        Log("Error", message);
        _serilogLogger.LogError(message);
    }

    public void LogError(Exception exception, string message)
    {
        Log("Error", $"{message}: {exception}");
        _serilogLogger.LogError(exception, message);
    }

    public void LogDebug(string message)
    {
        Log("Debug", message);
        _serilogLogger.LogDebug(message);
    }

    private void Log(string logLevel, string message)
    {
        var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] - {message}";
        File.AppendAllText(_filePath, logMessage + Environment.NewLine);
    }
}