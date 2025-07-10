using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.ScanLog.Extensions;
using Classic.ScanLog.Models;
using Classic.ScanLog.Parsers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Demo;

/// <summary>
///     Simple demonstration of the ScanLog library functionality
/// </summary>
public class ScanLogDemo
{
    public static async Task Main(string[] args)
    {
        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Configure services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        // Add ScanLog services with configuration
        services.AddScanLogServices(config =>
        {
            config.EnableFcxMode = false;
            config.AutoOpenResults = false;
            config.MaxConcurrentLogs = Environment.ProcessorCount;
            config.PreferredMode = ProcessingMode.Adaptive;
        });

        // Add a simple message handler
        services.AddSingleton<IMessageHandler, SimpleMessageHandler>();

        var serviceProvider = services.BuildServiceProvider();

        // Get the logger
        var logger = serviceProvider.GetRequiredService<ILogger<ScanLogDemo>>();

        try
        {
            logger.LogInformation("CLASSIC-8 ScanLog Demo Starting...");

            // Get the crash log parser
            var parser = serviceProvider.GetRequiredService<ICrashLogParser>();

            // Find crash logs in the Crash Logs directory
            var crashLogDir = Path.Combine(Directory.GetCurrentDirectory(), "Crash Logs");
            if (!Directory.Exists(crashLogDir))
            {
                logger.LogWarning("Crash Logs directory not found at: {CrashLogDir}", crashLogDir);
                return;
            }

            var logFiles = Directory.GetFiles(crashLogDir, "*.log");
            if (logFiles.Length == 0)
            {
                logger.LogWarning("No crash log files found in: {CrashLogDir}", crashLogDir);
                return;
            }

            logger.LogInformation("Found {Count} crash log files", logFiles.Length);

            // Parse the first crash log as a demo
            var firstLogFile = logFiles[0];
            logger.LogInformation("Parsing crash log: {FileName}", Path.GetFileName(firstLogFile));

            var crashLog = await parser.ParseCrashLogAsync(firstLogFile);

            logger.LogInformation("Successfully parsed crash log:");
            logger.LogInformation("  File: {FileName}", crashLog.FileName);
            logger.LogInformation("  Game Version: {GameVersion}", crashLog.GameVersion);
            logger.LogInformation("  Crash Generator: {CrashGenVersion}", crashLog.CrashGenVersion);
            logger.LogInformation("  Main Error: {MainError}", crashLog.MainError);
            logger.LogInformation("  Segments found: {SegmentCount}", crashLog.Segments.Count);

            foreach (var segment in crashLog.Segments)
                logger.LogInformation("    - {SegmentName}: {LineCount} lines", segment.Key, segment.Value.Count);

            // Test the extension methods
            if (crashLog.HasValidPluginList())
            {
                var (light, regular, total) = crashLog.GetPluginCounts();
                logger.LogInformation("  Plugin counts: Light={Light}, Regular={Regular}, Total={Total}",
                    light, regular, total);
            }
            else
            {
                logger.LogWarning("  No valid plugin list found in crash log");
            }

            // Test system specs extraction
            var systemSpecs = crashLog.ExtractSystemSpecs();
            logger.LogInformation("  System specs:");
            logger.LogInformation("    OS: {OS}", systemSpecs.OperatingSystem);
            logger.LogInformation("    CPU: {CPU}", systemSpecs.Cpu);
            logger.LogInformation("    Memory: {Memory}", systemSpecs.MemoryInfo);
            logger.LogInformation("    GPUs: {GPUCount}", systemSpecs.GpUs.Count);

            logger.LogInformation("Demo completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo failed with error");
        }
    }
}

/// <summary>
///     Simple message handler implementation for demo purposes
/// </summary>
public class SimpleMessageHandler : IMessageHandler
{
    private readonly ILogger<SimpleMessageHandler> _logger;

    public SimpleMessageHandler(ILogger<SimpleMessageHandler> logger)
    {
        _logger = logger;
    }

    public void SendMessage(string message, MessageType type, MessageTarget target)
    {
        _logger.LogInformation("[{Type}] {Message}", type, message);
    }

    public void ReportProgress(string operation, int current, int total)
    {
        _logger.LogInformation("[Progress] {Operation}: {Current}/{Total}", operation, current, total);
    }

    public IDisposable BeginProgressContext(string operation, int total)
    {
        _logger.LogInformation("[Progress] Starting: {Operation} (Total: {Total})", operation, total);
        return new ProgressContext();
    }

    public Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Async] {Message}", message);
        return Task.CompletedTask;
    }

    public Task SendProgressAsync(int current, int total, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Async Progress] {Current}/{Total}: {Message}", current, total, message);
        return Task.CompletedTask;
    }

    private class ProgressContext : IDisposable
    {
        public void Dispose() { }
    }
}
