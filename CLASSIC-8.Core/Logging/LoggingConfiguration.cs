using NLog;
using NLog.Targets;

namespace CLASSIC_8.Core.Logging;

/// <summary>
///     Provides logging configuration for the CLASSIC application.
/// </summary>
public static class LoggingConfiguration
{
    private const string LogFileName = "CLASSIC Journal.log";
    private const int LogRetentionDays = 7;

    /// <summary>
    ///     Configures NLog for the application.
    /// </summary>
    public static void Configure()
    {
        var config = new NLog.Config.LoggingConfiguration();

        // Check if log file exists and is older than 7 days
        var logPath = Path.Combine(Directory.GetCurrentDirectory(), LogFileName);
        var logAge = TimeSpan.Zero;

        if (File.Exists(logPath))
        {
            var logTime = File.GetLastWriteTime(logPath);
            logAge = DateTime.Now - logTime;

            if (logAge.TotalDays > LogRetentionDays)
                try
                {
                    File.Delete(logPath);
                    // Log will be recreated with the message below
                }
                catch (Exception ex)
                {
                    // Can't log yet, so just continue
                    Console.WriteLine($"Failed to delete old log file: {ex.Message}");
                }
        }

        // Create file target
        var fileTarget = new FileTarget("file")
        {
            FileName = LogFileName,
            Layout =
                "${longdate} | ${level:uppercase=true} | ${message}${onexception:${newline}${exception:format=tostring}}",
            KeepFileOpen = false,
            ConcurrentWrites = true,
            ArchiveAboveSize = 10485760, // 10MB
            ArchiveNumbering = ArchiveNumberingMode.Rolling,
            MaxArchiveFiles = 3
        };

        // Add the target to the configuration
        config.AddTarget(fileTarget);

        // Define rules
        config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget, "CLASSIC*");
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget, "CLASSIC*");

        // Apply configuration
        LogManager.Configuration = config;

        // Log initialization
        var logger = LogManager.GetLogger("CLASSIC");
        logger.Debug("- - - INITIATED LOGGING CHECK");

        if (logAge.TotalDays > LogRetentionDays)
            logger.Info("CLASSIC Journal.log has been deleted and regenerated due to being older than 7 days.");
    }

    /// <summary>
    ///     Shuts down the logging system gracefully.
    /// </summary>
    public static void Shutdown()
    {
        LogManager.Shutdown();
    }
}