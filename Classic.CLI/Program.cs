using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.ScanLog.Extensions;
using Classic.ScanLog.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Classic.CLI;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("CLASSIC-8 CLI - Crash Log Analysis Tool");
        Console.WriteLine("========================================");

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
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("CLASSIC-8 ScanLog CLI Starting...");

            // Get the crash log parser
            var parser = serviceProvider.GetRequiredService<ICrashLogParser>();

            // Find crash logs directory
            var crashLogsPath = Path.Combine(Directory.GetCurrentDirectory(), "Crash Logs");
            if (!Directory.Exists(crashLogsPath))
            {
                logger.LogError("Crash Logs directory not found at: {Path}", crashLogsPath);
                return;
            }

            // Get first .log file for demo
            var logFiles = Directory.GetFiles(crashLogsPath, "*.log");
            if (logFiles.Length == 0)
            {
                logger.LogWarning("No crash log files found in: {Path}", crashLogsPath);
                return;
            }

            // Test with a log file that has plugin data
            var testLogFile = logFiles.FirstOrDefault(f => f.Contains("2023-10-06")) ?? logFiles[0];
            logger.LogInformation("Parsing crash log: {FileName}", Path.GetFileName(testLogFile));

            // Parse the crash log
            var crashLog = await parser.ParseCrashLogAsync(testLogFile);

            // Display basic results
            Console.WriteLine();
            Console.WriteLine($"Successfully parsed: {crashLog.FileName}");
            Console.WriteLine($"File Path: {crashLog.FilePath}");
            Console.WriteLine($"Date Created: {crashLog.DateCreated}");
            Console.WriteLine($"Game Version: {crashLog.GameVersion}");
            Console.WriteLine($"CrashGen Version: {crashLog.CrashGenVersion}");

            if (!string.IsNullOrEmpty(crashLog.MainError)) Console.WriteLine($"Main Error: {crashLog.MainError}");

            if (crashLog.RawContent.Count > 0) Console.WriteLine($"Raw Content Lines: {crashLog.RawContent.Count}");

            if (crashLog.Segments.Count > 0)
            {
                Console.WriteLine($"Segments Found: {crashLog.Segments.Count}");
                Console.WriteLine("All Segment Names:");
                foreach (var segment in crashLog.Segments.Keys)
                    Console.WriteLine($"  - {segment} ({crashLog.Segments[segment].Count} lines)");

                // Test plugin analysis if plugins segment exists
                if (crashLog.Segments.ContainsKey("Plugins"))
                {
                    Console.WriteLine();
                    Console.WriteLine("Testing Plugin Analysis...");

                    var pluginAnalyzer = serviceProvider.GetRequiredService<IPluginAnalyzer>();
                    await pluginAnalyzer.AnalyzePluginsAsync(crashLog);

                    Console.WriteLine("Plugin analysis completed successfully.");

                    // Test load order validation
                    var validationIssues = await pluginAnalyzer.ValidateLoadOrderAsync(crashLog);
                    if (validationIssues.Count > 0)
                    {
                        Console.WriteLine("Load order validation issues found:");
                        foreach (var issue in validationIssues.Take(3)) Console.WriteLine($"  - {issue}");
                    }
                    else
                    {
                        Console.WriteLine("No load order issues detected.");
                    }
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("No Plugins segment found for analysis.");
                }
            }

            logger.LogInformation("Demo completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo failed with error: {Message}", ex.Message);
        }
    }
}

/// <summary>
///     Simple message handler for CLI output
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
