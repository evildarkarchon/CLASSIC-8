using System.CommandLine;
using System.CommandLine.Invocation;
using Classic.Core.Interfaces;
using Classic.Infrastructure.Extensions;
using Classic.ScanLog.Extensions;
using Classic.ScanLog.Models;
using Classic.ScanLog.Reporting;
using Classic.ScanLog.Utilities;
using Classic.ScanLog.Parsers;
using Classic.ScanLog.Analyzers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Classic.CLI.Commands;

/// <summary>
/// Command to generate advanced reports from crash logs
/// </summary>
public class GenerateReportCommand : Command
{
    public GenerateReportCommand() : base("generate-report", "Generate advanced crash log analysis report")
    {
        var logPathArgument = new Argument<string>("logPath", "Path to the crash log file");

        var outputOption = new Option<string?>(
            ["--output", "-o"],
            "Output file path for the report (default: same directory as log file)");

        var formatOption = new Option<string>(
            ["--format", "-f"],
            () => "markdown",
            "Report format (markdown, text, html)");

        var verboseOption = new Option<bool>(
            ["--verbose", "-v"],
            "Enable verbose logging");

        var quietOption = new Option<bool>(
            ["--quiet", "-q"],
            "Suppress non-essential output");

        AddArgument(logPathArgument);
        AddOption(outputOption);
        AddOption(formatOption);
        AddOption(verboseOption);
        AddOption(quietOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var logPath = context.ParseResult.GetValueForArgument(logPathArgument);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var format = context.ParseResult.GetValueForOption(formatOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            await ExecuteAsync(context, logPath, output, format ?? "markdown", verbose, quiet,
                context.GetCancellationToken());
        });
    }

    private async Task ExecuteAsync(
        InvocationContext context,
        string logPath,
        string? output,
        string format,
        bool verbose,
        bool quiet,
        CancellationToken cancellationToken)
    {
        // Configure logging
        var logLevel = verbose ? LogEventLevel.Debug : quiet ? LogEventLevel.Warning : LogEventLevel.Information;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console()
            .CreateLogger();

        var logger = Log.Logger;

        try
        {
            // Validate input file
            if (!File.Exists(logPath))
            {
                logger.Error("Crash log file not found: {LogPath}", logPath);
                context.ExitCode = 1;
                return;
            }

            // Configure services
            var services = new ServiceCollection();

            // Add logging services  
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.SetMinimumLevel(logLevel switch
                {
                    LogEventLevel.Debug => LogLevel.Debug,
                    LogEventLevel.Warning => LogLevel.Warning,
                    _ => LogLevel.Information
                });
            });

            // Add infrastructure and scanning services
            services.AddClassicInfrastructure();
            services.AddScanLogServices();

            var serviceProvider = services.BuildServiceProvider();

            logger.Information("Starting advanced report generation for: {LogPath}", logPath);

            // Get required services
            var crashLogParser = serviceProvider.GetRequiredService<ICrashLogParser>();
            var suspectScanner = serviceProvider.GetRequiredService<EnhancedSuspectScanner>();
            var modConflictDetector = serviceProvider.GetRequiredService<ModConflictDetector>();
            var reportGenerator = serviceProvider.GetRequiredService<AdvancedReportGenerator>();
            var reportDataMapper = serviceProvider.GetRequiredService<ReportDataMapper>();

            var startTime = DateTime.Now;

            // Parse crash log
            logger.Information("Parsing crash log...");
            var crashLog = await crashLogParser.ParseCrashLogAsync(logPath, cancellationToken);

            // Run analysis
            logger.Information("Running crash analysis...");
            var suspects = await suspectScanner.ScanForSuspectsAsync(crashLog, cancellationToken);

            // Create crash log data for mod conflict detection
            var crashLogData = new CrashLogData
            {
                MainError = crashLog.MainError,
                SystemSpecs = crashLog.Segments.TryGetValue("SystemSpecs", out var systemSpecs)
                    ? string.Join("\n", systemSpecs)
                    : null,
                CallStack = crashLog.Segments.TryGetValue("CallStack", out var callStack)
                    ? string.Join("\n", callStack)
                    : null,
                Plugins = crashLog.Plugins,
                Headers = new Dictionary<string, object>
                {
                    ["GameVersion"] = crashLog.GameVersion,
                    ["CrashGenVersion"] = crashLog.CrashGenVersion,
                    ["FileName"] = crashLog.FileName
                }
            };

            var modConflicts = await modConflictDetector.DetectModConflictsAsync(crashLogData);

            var processingTime = DateTime.Now - startTime;

            // Map to report data
            logger.Information("Generating advanced report...");
            var fileName = Path.GetFileName(logPath);

            // Convert DetectedSuspect to Suspect
            var suspectsList = suspects.Select(ds => new Core.Models.Suspect
            {
                Name = ds.Name,
                Description = ds.Description,
                SeverityScore = ds.Severity,
                Evidence = string.Join(", ", ds.MatchedPatterns),
                Recommendation = string.Join("; ", ds.Solutions),
                Confidence = ds.Confidence,
                Type = Core.Models.SuspectType.Unknown,
                Severity = ds.Severity switch
                {
                    >= 5 => Core.Models.SeverityLevel.Critical,
                    >= 4 => Core.Models.SeverityLevel.High,
                    >= 3 => Core.Models.SeverityLevel.Medium,
                    _ => Core.Models.SeverityLevel.Low
                }
            }).ToList();

            var reportData = reportDataMapper.MapCrashLogAnalysisToReportData(
                crashLog, suspectsList, modConflicts, new List<FileValidationResult>(),
                fileName, processingTime);

            // Generate report
            var reportContent = await reportGenerator.GenerateComprehensiveReportAsync(reportData, cancellationToken);

            // Determine output path
            var outputPath = output ?? Path.ChangeExtension(logPath, GetReportExtension(format));

            // Write report
            await File.WriteAllTextAsync(outputPath, reportContent, cancellationToken);

            logger.Information("Advanced report generated successfully!");
            logger.Information("Report saved to: {OutputPath}", outputPath);
            logger.Information("Processing completed in {Duration:F2} seconds", processingTime.TotalSeconds);

            // Print summary
            PrintReportSummary(reportData, logger);

            context.ExitCode = 0;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Report generation failed");
            context.ExitCode = 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private void PrintReportSummary(AdvancedReportData reportData, ILogger logger)
    {
        logger.Information("=== REPORT SUMMARY ===");
        logger.Information("File: {FileName}", reportData.FileName);
        logger.Information("Game: {GameId} v{GameVersion}", reportData.GameInfo.GameId,
            reportData.GameInfo.GameVersion);
        logger.Information("Crash Reporter: {CrashGen} v{Version}",
            reportData.GameInfo.CrashGenName, reportData.GameInfo.CrashGenVersion);
        logger.Information("");
        logger.Information("Analysis Results:");
        logger.Information("  Critical Issues: {Critical}", reportData.CriticalIssues.Count);
        logger.Information("  Errors: {Errors}", reportData.Errors.Count);
        logger.Information("  Warnings: {Warnings}", reportData.Warnings.Count);
        logger.Information("  Crash Suspects: {Suspects}", reportData.CrashSuspects.Count);
        logger.Information("  Mod Conflicts: {ModConflicts}", reportData.ModConflicts.Count);

        if (reportData.CrashSuspects.Any())
        {
            var topSuspect = reportData.CrashSuspects.OrderByDescending(s => s.SeverityScore).First();
            logger.Information("  Top Suspect: {Suspect} (Severity: {Severity})",
                topSuspect.Name, topSuspect.SeverityScore);
        }

        // Overall assessment
        var totalIssues = reportData.CriticalIssues.Count + reportData.Errors.Count + reportData.Warnings.Count;
        if (totalIssues == 0)
            logger.Information("✅ No major issues detected!");
        else if (reportData.CriticalIssues.Any())
            logger.Warning("🚨 Critical issues detected - immediate action required");
        else if (reportData.Errors.Any())
            logger.Warning("⚠️ Errors found - should be addressed for stability");
        else
            logger.Information("ℹ️ Minor warnings found - consider addressing");
    }

    private string GetReportExtension(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "html" => ".html",
            "text" => ".txt",
            "markdown" or "md" => ".md",
            _ => ".md"
        };
    }
}
