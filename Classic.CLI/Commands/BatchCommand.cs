using System.CommandLine;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.Infrastructure.Extensions;
using Classic.ScanLog.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Classic.CLI.Commands;

public class BatchCommand : Command
{
    public BatchCommand() : base("batch", "Process multiple crash logs from a file list")
    {
        var inputFileOption = new Option<FileInfo>(
            ["--input", "-i"],
            description: "Path to text file containing list of crash log paths")
        {
            IsRequired = true
        };

        var outputDirOption = new Option<DirectoryInfo>(
            ["--output", "-o"],
            description: "Output directory for reports")
        {
            IsRequired = true
        };

        var parallelOption = new Option<int>(
            ["--parallel", "-p"],
            getDefaultValue: () => Environment.ProcessorCount,
            description: "Number of parallel workers");

        var stopOnErrorOption = new Option<bool>(
            ["--stop-on-error", "-e"],
            getDefaultValue: () => false,
            description: "Stop processing if a log fails");

        var summaryOption = new Option<bool>(
            ["--summary", "-s"],
            getDefaultValue: () => true,
            description: "Generate summary report");

        AddOption(inputFileOption);
        AddOption(outputDirOption);
        AddOption(parallelOption);
        AddOption(stopOnErrorOption);
        AddOption(summaryOption);

        this.SetHandler(async (context) =>
        {
            var inputFile = context.ParseResult.GetValueForOption(inputFileOption)!;
            var outputDir = context.ParseResult.GetValueForOption(outputDirOption)!;
            var parallel = context.ParseResult.GetValueForOption(parallelOption);
            var stopOnError = context.ParseResult.GetValueForOption(stopOnErrorOption);
            var summary = context.ParseResult.GetValueForOption(summaryOption);

            await ExecuteAsync(inputFile, outputDir, parallel, stopOnError, summary,
                context.GetCancellationToken());
        });
    }

    private async Task ExecuteAsync(
        FileInfo inputFile,
        DirectoryInfo outputDir,
        int parallel,
        bool stopOnError,
        bool summary,
        CancellationToken cancellationToken)
    {
        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(Path.Combine(outputDir.FullName, "batch-processing-.log"), 
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Configure services
        var services = new ServiceCollection();

        // Add infrastructure services
        services.AddClassicInfrastructure();

        // Add ScanLog services
        services.AddScanLogServices();

        // Add CLI message handler
        services.AddSingleton<IMessageHandler, CliMessageHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var logger = Log.ForContext<BatchCommand>();

        try
        {
            Console.WriteLine("CLASSIC-8 Batch Processor");
            Console.WriteLine("========================");
            Console.WriteLine();

            // Read input file
            if (!inputFile.Exists)
            {
                logger.Error("Input file not found: {Path}", inputFile.FullName);
                Environment.Exit(1);
                return;
            }

            var logPaths = await File.ReadAllLinesAsync(inputFile.FullName, cancellationToken);
            logPaths = logPaths.Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p)).ToArray();

            if (logPaths.Length == 0)
            {
                logger.Error("No valid log files found in input file");
                Environment.Exit(1);
                return;
            }

            logger.Information("Found {Count} log files to process", logPaths.Length);

            // Create output directory
            outputDir.Create();

            // Get services
            var orchestrator = serviceProvider.GetRequiredService<IScanOrchestrator>();
            var messageHandler = serviceProvider.GetRequiredService<IMessageHandler>();

            // Process logs using single-log scan
            var startTime = DateTime.Now;
            var results = new List<BatchResult>();

            using var semaphore = new SemaphoreSlim(parallel);
            var tasks = logPaths.Select(async logPath =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var result = await ProcessSingleLogAsync(
                        logPath, outputDir.FullName, orchestrator, cancellationToken);
                    
                    lock (results)
                    {
                        results.Add(result);
                        messageHandler.ReportProgress("Processing logs", results.Count, logPaths.Length);
                    }

                    if (!result.Success && stopOnError)
                    {
                        throw new Exception($"Failed to process: {logPath}");
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            var elapsed = DateTime.Now - startTime;

            // Generate summary report
            if (summary)
            {
                await GenerateSummaryReportAsync(outputDir.FullName, results, elapsed);
            }

            // Display results
            Console.WriteLine();
            Console.WriteLine("Batch Processing Complete!");
            Console.WriteLine("=========================");
            Console.WriteLine($"Total logs: {results.Count}");
            Console.WriteLine($"Successful: {results.Count(r => r.Success)}");
            Console.WriteLine($"Failed: {results.Count(r => !r.Success)}");
            Console.WriteLine($"Processing time: {elapsed:mm\\:ss}");
            Console.WriteLine($"Reports saved to: {outputDir.FullName}");

            var failureCount = results.Count(r => !r.Success);
            Environment.Exit(failureCount > 0 ? 1 : 0);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Batch processing failed");
            Environment.Exit(1);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static async Task<BatchResult> ProcessSingleLogAsync(
        string logPath,
        string outputDir,
        IScanOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        var logger = Log.ForContext("LogPath", logPath);
        var result = new BatchResult
        {
            LogPath = logPath,
            StartTime = DateTime.Now
        };

        try
        {
            // Use the orchestrator to scan single log
            var scanResult = await orchestrator.ScanSingleLogAsync(logPath, cancellationToken);
            
            // Generate simple report
            var reportPath = Path.Combine(outputDir, 
                Path.GetFileNameWithoutExtension(logPath) + "-BATCH.md");
            
            await GenerateLogReportAsync(reportPath, scanResult);
            
            result.Success = scanResult.IsSuccessful;
            result.ReportPath = reportPath;
            result.Error = scanResult.ErrorMessage;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to process log: {Path}", logPath);
            result.Success = false;
            result.Error = ex.Message;
        }

        result.EndTime = DateTime.Now;
        return result;
    }

    private static async Task GenerateLogReportAsync(string reportPath, ScanLogResult scanResult)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("# Crash Log Analysis Report");
        report.AppendLine($"**File:** {scanResult.LogFileName}");
        report.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"**Status:** {(scanResult.IsSuccessful ? "Success" : "Failed")}");
        report.AppendLine();
        
        if (scanResult.IsSuccessful)
        {
            report.AppendLine("## Summary");
            report.AppendLine($"- Game: {scanResult.GameId}");
            report.AppendLine($"- Game Version: {scanResult.GameVersion}");
            report.AppendLine($"- Processing Time: {scanResult.Duration.TotalSeconds:F2}s");
            report.AppendLine($"- Plugins Found: {scanResult.PluginCount}");
            report.AppendLine($"- FormIDs Found: {scanResult.FormIdCount}");
            report.AppendLine($"- Suspects Found: {scanResult.SuspectCount}");
            
            if (!string.IsNullOrEmpty(scanResult.MainError))
            {
                report.AppendLine();
                report.AppendLine("## Main Error");
                report.AppendLine($"```");
                report.AppendLine(scanResult.MainError);
                report.AppendLine($"```");
            }
            
            if (scanResult.Suspects.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("## Suspects");
                foreach (var suspect in scanResult.Suspects)
                {
                    report.AppendLine($"- {suspect}");
                }
            }
        }
        else
        {
            report.AppendLine("## Error");
            report.AppendLine($"Failed to process: {scanResult.ErrorMessage}");
        }
        
        await File.WriteAllTextAsync(reportPath, report.ToString());
    }

    private static async Task GenerateSummaryReportAsync(
        string outputDir,
        List<BatchResult> results,
        TimeSpan elapsed)
    {
        var summaryPath = Path.Combine(outputDir, $"BatchSummary_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.md");
        var report = new System.Text.StringBuilder();

        report.AppendLine("# CLASSIC-8 Batch Processing Summary");
        report.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"**Total Processing Time:** {elapsed:mm\\:ss}");
        report.AppendLine();

        report.AppendLine("## Statistics");
        report.AppendLine($"- Total logs processed: {results.Count}");
        report.AppendLine($"- Successful: {results.Count(r => r.Success)}");
        report.AppendLine($"- Failed: {results.Count(r => !r.Success)}");
        report.AppendLine($"- Average processing time: {results.Average(r => (r.EndTime - r.StartTime).TotalSeconds):F2} seconds");
        report.AppendLine();

        if (results.Any(r => r.Success))
        {
            report.AppendLine("## Successfully Processed");
            foreach (var result in results.Where(r => r.Success))
            {
                report.AppendLine($"- `{Path.GetFileName(result.LogPath)}`");
            }
            report.AppendLine();
        }

        if (results.Any(r => !r.Success))
        {
            report.AppendLine("## Failed Processing");
            foreach (var result in results.Where(r => !r.Success))
            {
                report.AppendLine($"- `{Path.GetFileName(result.LogPath)}`: {result.Error}");
            }
            report.AppendLine();
        }

        report.AppendLine("## Processing Details");
        report.AppendLine("| Log File | Status | Processing Time | Report |");
        report.AppendLine("|----------|--------|-----------------|--------|");
        
        foreach (var result in results.OrderBy(r => r.StartTime))
        {
            var status = result.Success ? "✓ Success" : "✗ Failed";
            var time = (result.EndTime - result.StartTime).TotalSeconds;
            var reportLink = result.Success && !string.IsNullOrEmpty(result.ReportPath) 
                ? $"[View]({Path.GetFileName(result.ReportPath)})" 
                : "N/A";
            
            report.AppendLine($"| {Path.GetFileName(result.LogPath)} | {status} | {time:F2}s | {reportLink} |");
        }

        await File.WriteAllTextAsync(summaryPath, report.ToString());
    }

    private class BatchResult
    {
        public string LogPath { get; init; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? ReportPath { get; set; }
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; set; }
    }
}