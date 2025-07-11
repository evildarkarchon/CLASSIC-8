using System.CommandLine;
using System.CommandLine.Invocation;
using Classic.Infrastructure.Extensions;
using Classic.ScanLog.Extensions;
using Classic.ScanLog.Models;
using Classic.ScanLog.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Classic.CLI.Commands;

/// <summary>
/// Command to validate game files for format issues and conflicts
/// </summary>
public class ValidateFilesCommand : Command
{
    public ValidateFilesCommand() : base("validate-files", "Validate game files for format issues and conflicts")
    {
        var pathArgument = new Argument<string>("path", "Path to directory containing game files to validate");

        var outputOption = new Option<string?>(
            ["--output", "-o"],
            "Output directory for validation reports");

        var recursiveOption = new Option<bool>(
            ["--recursive", "-r"],
            () => true,
            "Recursively scan subdirectories");

        var texturesOnlyOption = new Option<bool>(
            ["--textures-only"],
            "Validate only texture files");

        var archivesOnlyOption = new Option<bool>(
            ["--archives-only"],
            "Validate only archive files");

        var audioOnlyOption = new Option<bool>(
            ["--audio-only"],
            "Validate only audio files");

        var scriptsOnlyOption = new Option<bool>(
            ["--scripts-only"],
            "Validate only script files");

        var verboseOption = new Option<bool>(
            ["--verbose", "-v"],
            "Enable verbose logging");

        var quietOption = new Option<bool>(
            ["--quiet", "-q"],
            "Suppress non-essential output");

        AddArgument(pathArgument);
        AddOption(outputOption);
        AddOption(recursiveOption);
        AddOption(texturesOnlyOption);
        AddOption(archivesOnlyOption);
        AddOption(audioOnlyOption);
        AddOption(scriptsOnlyOption);
        AddOption(verboseOption);
        AddOption(quietOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var path = context.ParseResult.GetValueForArgument(pathArgument);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var recursive = context.ParseResult.GetValueForOption(recursiveOption);
            var texturesOnly = context.ParseResult.GetValueForOption(texturesOnlyOption);
            var archivesOnly = context.ParseResult.GetValueForOption(archivesOnlyOption);
            var audioOnly = context.ParseResult.GetValueForOption(audioOnlyOption);
            var scriptsOnly = context.ParseResult.GetValueForOption(scriptsOnlyOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            await ExecuteAsync(context, path, output, recursive, texturesOnly, archivesOnly, 
                audioOnly, scriptsOnly, verbose, quiet, context.GetCancellationToken());
        });
    }

    private async Task ExecuteAsync(
        InvocationContext context,
        string path,
        string? output,
        bool recursive,
        bool texturesOnly,
        bool archivesOnly,
        bool audioOnly,
        bool scriptsOnly,
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
            // Validate input path
            if (!Directory.Exists(path))
            {
                logger.Error("Directory not found: {Path}", path);
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

            // Get validators
            var gameFileValidator = serviceProvider.GetRequiredService<GameFileValidator>();
            var textureValidator = serviceProvider.GetRequiredService<TextureValidator>();
            var archiveValidator = serviceProvider.GetRequiredService<ArchiveValidator>();
            var audioValidator = serviceProvider.GetRequiredService<AudioValidator>();
            var scriptValidator = serviceProvider.GetRequiredService<ScriptValidator>();

            logger.Information("Starting file validation for: {Path}", path);

            // Determine output directory
            var outputDirectory = output ?? Path.Combine(path, "ValidationReports");
            Directory.CreateDirectory(outputDirectory);

            if (texturesOnly || archivesOnly || audioOnly || scriptsOnly)
            {
                // Validate specific file types only
                await ValidateSpecificTypesAsync(
                    path, outputDirectory, texturesOnly, archivesOnly, audioOnly, scriptsOnly,
                    textureValidator, archiveValidator, audioValidator, scriptValidator,
                    logger, cancellationToken);
            }
            else
            {
                // Validate all file types
                var summary = await gameFileValidator.ValidateDirectoryAsync(path, cancellationToken);
                await GenerateValidationReportAsync(summary, outputDirectory, logger, cancellationToken);
                
                // Print summary
                PrintValidationSummary(summary, logger);
                
                // Set exit code based on results
                context.ExitCode = summary.CriticalIssues > 0 ? 2 : summary.ErrorIssues > 0 ? 1 : 0;
            }

            logger.Information("File validation completed successfully");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "File validation failed");
            context.ExitCode = 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private async Task ValidateSpecificTypesAsync(
        string path,
        string outputDirectory,
        bool texturesOnly,
        bool archivesOnly,
        bool audioOnly,
        bool scriptsOnly,
        TextureValidator textureValidator,
        ArchiveValidator archiveValidator,
        AudioValidator audioValidator,
        ScriptValidator scriptValidator,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var allFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Select(f => (f, Path.GetRelativePath(path, f)))
            .ToList();

        if (texturesOnly)
        {
            var textureFiles = allFiles.Where(f => IsTextureFile(f.f)).ToList();
            logger.Information("Validating {Count} texture files", textureFiles.Count);
            
            var results = await textureValidator.ValidateTexturesAsync(textureFiles, cancellationToken);
            await WriteValidationResultsAsync(results, Path.Combine(outputDirectory, "texture-validation.txt"), logger);
        }

        if (archivesOnly)
        {
            var archiveFiles = allFiles.Where(f => ArchiveValidator.IsSupportedArchive(f.f)).ToList();
            logger.Information("Validating {Count} archive files", archiveFiles.Count);
            
            var results = await archiveValidator.ValidateArchivesAsync(archiveFiles, cancellationToken);
            await WriteValidationResultsAsync(results, Path.Combine(outputDirectory, "archive-validation.txt"), logger);
        }

        if (audioOnly)
        {
            var audioFiles = allFiles.Where(f => AudioValidator.IsAudioFile(f.f)).ToList();
            logger.Information("Validating {Count} audio files", audioFiles.Count);
            
            var results = await audioValidator.ValidateAudioFilesAsync(audioFiles, cancellationToken);
            await WriteValidationResultsAsync(results, Path.Combine(outputDirectory, "audio-validation.txt"), logger);
        }

        if (scriptsOnly)
        {
            var scriptFiles = allFiles.Where(f => ScriptValidator.IsScriptFile(f.f)).ToList();
            logger.Information("Validating {Count} script files", scriptFiles.Count);
            
            var results = await scriptValidator.ValidateScriptsAsync(scriptFiles, cancellationToken);
            await WriteValidationResultsAsync(results, Path.Combine(outputDirectory, "script-validation.txt"), logger);
        }
    }

    private async Task WriteValidationResultsAsync<T>(
        List<T> results, 
        string outputPath, 
        ILogger logger) where T : FileValidationResult
    {
        try
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"# File Validation Report - {typeof(T).Name}");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Total files validated: {results.Count}");
            report.AppendLine();

            var issueResults = results.Where(r => r.Status != ValidationStatus.Valid).ToList();
            
            if (issueResults.Any())
            {
                report.AppendLine("## Issues Found");
                report.AppendLine();
                
                foreach (var result in issueResults.OrderByDescending(r => r.Status))
                {
                    report.AppendLine($"### {result.Status}: {result.RelativePath}");
                    if (!string.IsNullOrEmpty(result.Issue))
                        report.AppendLine($"**Issue**: {result.Issue}");
                    if (!string.IsNullOrEmpty(result.Description))
                        report.AppendLine($"**Description**: {result.Description}");
                    if (!string.IsNullOrEmpty(result.Recommendation))
                        report.AppendLine($"**Recommendation**: {result.Recommendation}");
                    report.AppendLine();
                }
            }
            else
            {
                report.AppendLine("## No Issues Found");
                report.AppendLine("All validated files passed validation checks.");
            }

            await File.WriteAllTextAsync(outputPath, report.ToString());
            logger.Information("Validation report written to: {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to write validation report to: {OutputPath}", outputPath);
        }
    }

    private async Task GenerateValidationReportAsync(
        GameFileValidationSummary summary,
        string outputDirectory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var reportPath = Path.Combine(outputDirectory, $"file-validation-report-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.md");
            
            var report = new System.Text.StringBuilder();
            report.AppendLine("# Game File Validation Report");
            report.AppendLine($"**Directory**: {summary.DirectoryPath}");
            report.AppendLine($"**Generated**: {summary.EndTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"**Duration**: {summary.Duration:mm\\:ss}");
            report.AppendLine();
            
            report.AppendLine("## Summary");
            report.AppendLine($"- **Total Files Scanned**: {summary.TotalFilesScanned:N0}");
            report.AppendLine($"- **Total Issues**: {summary.TotalIssues:N0}");
            report.AppendLine($"- **Critical Issues**: {summary.CriticalIssues:N0}");
            report.AppendLine($"- **Errors**: {summary.ErrorIssues:N0}");
            report.AppendLine($"- **Warnings**: {summary.WarningIssues:N0}");
            report.AppendLine();

            // Add detailed results for each type
            AppendValidationResults(report, "Texture Issues", summary.TextureResults.Where(r => r.Status != ValidationStatus.Valid));
            AppendValidationResults(report, "Archive Issues", summary.ArchiveResults.Where(r => r.Status != ValidationStatus.Valid));
            AppendValidationResults(report, "Audio Issues", summary.AudioResults.Where(r => r.Status != ValidationStatus.Valid));
            AppendValidationResults(report, "Script Issues", summary.ScriptResults.Where(r => r.Status != ValidationStatus.Valid));
            AppendValidationResults(report, "Special Issues", summary.SpecialResults.Where(r => r.Status != ValidationStatus.Valid));

            await File.WriteAllTextAsync(reportPath, report.ToString(), cancellationToken);
            logger.Information("Comprehensive validation report written to: {ReportPath}", reportPath);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to generate validation report");
        }
    }

    private void AppendValidationResults<T>(System.Text.StringBuilder report, string sectionTitle, IEnumerable<T> results) where T : FileValidationResult
    {
        var resultList = results.ToList();
        if (!resultList.Any()) return;

        report.AppendLine($"## {sectionTitle}");
        report.AppendLine();

        foreach (var result in resultList.OrderByDescending(r => r.Status))
        {
            report.AppendLine($"### {result.Status}: {result.RelativePath}");
            if (!string.IsNullOrEmpty(result.Issue))
                report.AppendLine($"**Issue**: {result.Issue}");
            if (!string.IsNullOrEmpty(result.Description))
                report.AppendLine($"**Description**: {result.Description}");
            if (!string.IsNullOrEmpty(result.Recommendation))
                report.AppendLine($"**Recommendation**: {result.Recommendation}");
            report.AppendLine();
        }
    }

    private void PrintValidationSummary(GameFileValidationSummary summary, ILogger logger)
    {
        logger.Information("=== FILE VALIDATION SUMMARY ===");
        logger.Information("Directory: {DirectoryPath}", summary.DirectoryPath);
        logger.Information("Files Scanned: {TotalFiles:N0}", summary.TotalFilesScanned);
        logger.Information("Duration: {Duration:mm\\:ss}", summary.Duration);
        logger.Information("");
        logger.Information("Issues Found:");
        logger.Information("  Critical: {Critical:N0}", summary.CriticalIssues);
        logger.Information("  Errors: {Errors:N0}", summary.ErrorIssues);
        logger.Information("  Warnings: {Warnings:N0}", summary.WarningIssues);
        logger.Information("  Total: {Total:N0}", summary.TotalIssues);
        
        if (summary.TotalIssues == 0)
        {
            logger.Information("✅ All files passed validation!");
        }
        else if (summary.CriticalIssues > 0)
        {
            logger.Warning("❌ Critical issues found that require immediate attention");
        }
        else if (summary.ErrorIssues > 0)
        {
            logger.Warning("⚠️ Errors found that should be addressed");
        }
        else
        {
            logger.Information("ℹ️ Minor warnings found");
        }
    }

    private static bool IsTextureFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return new[] { ".dds", ".tga", ".png", ".jpg", ".jpeg", ".bmp" }.Contains(extension);
    }
}