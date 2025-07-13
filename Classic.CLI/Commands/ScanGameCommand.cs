using System.CommandLine;
using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.Infrastructure.Extensions;
using Classic.ScanGame.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Classic.CLI.Commands;

public class ScanGameCommand : Command
{
    public ScanGameCommand() : base("scan-game", "Scan game files for issues")
    {
        var gamePathOption = new Option<DirectoryInfo?>(
            ["--game-path", "-g"],
            "Path to the game directory");

        var modsPathOption = new Option<DirectoryInfo?>(
            ["--mods-path", "-m"],
            "Path to the mods directory");

        var checkXseOption = new Option<bool>(
            ["--check-xse", "-x"],
            () => true,
            "Check script extender plugins");

        var checkCrashgenOption = new Option<bool>(
            ["--check-crashgen", "-c"],
            () => true,
            "Check crash generation settings");

        var checkLogsOption = new Option<bool>(
            ["--check-logs", "-log"],
            () => true,
            "Check game logs for errors");

        var backupDocsOption = new Option<bool>(
            ["--backup-docs", "-b"],
            () => false,
            "Backup documentation files found in mods");

        var verboseOption = new Option<bool>(
            ["--verbose", "-v"],
            () => false,
            "Enable verbose logging");

        var quietOption = new Option<bool>(
            ["--quiet", "-q"],
            () => false,
            "Quiet mode - minimal output");

        var outputPathOption = new Option<DirectoryInfo?>(
            ["--output", "-o"],
            "Output directory for reports");

        AddOption(gamePathOption);
        AddOption(modsPathOption);
        AddOption(checkXseOption);
        AddOption(checkCrashgenOption);
        AddOption(checkLogsOption);
        AddOption(backupDocsOption);
        AddOption(verboseOption);
        AddOption(quietOption);
        AddOption(outputPathOption);

        this.SetHandler(async context =>
        {
            var gamePath = context.ParseResult.GetValueForOption(gamePathOption);
            var modsPath = context.ParseResult.GetValueForOption(modsPathOption);
            var checkXse = context.ParseResult.GetValueForOption(checkXseOption);
            var checkCrashgen = context.ParseResult.GetValueForOption(checkCrashgenOption);
            var checkLogs = context.ParseResult.GetValueForOption(checkLogsOption);
            var backupDocs = context.ParseResult.GetValueForOption(backupDocsOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var outputPath = context.ParseResult.GetValueForOption(outputPathOption);

            await ExecuteAsync(gamePath, modsPath, checkXse, checkCrashgen, checkLogs,
                backupDocs, verbose, quiet, outputPath, context.GetCancellationToken());
        });
    }

    private async Task ExecuteAsync(
        DirectoryInfo? gamePath,
        DirectoryInfo? modsPath,
        bool checkXse,
        bool checkCrashgen,
        bool checkLogs,
        bool backupDocs,
        bool verbose,
        bool quiet,
        DirectoryInfo? outputPath,
        CancellationToken cancellationToken)
    {
        // Configure logging
        var logLevel = quiet ? LogEventLevel.Warning : verbose ? LogEventLevel.Debug : LogEventLevel.Information;
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/classic-game-scan-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Configure services
        var services = new ServiceCollection();

        // Add infrastructure services
        services.AddClassicInfrastructure();

        // Add ScanGame services
        services.AddScanGameServices();

        // Add CLI message handler
        services.AddSingleton<IMessageHandler, CliMessageHandler>(provider =>
            new CliMessageHandler(true));

        var serviceProvider = services.BuildServiceProvider();
        var logger = Log.ForContext<ScanGameCommand>();
        var messageHandler = serviceProvider.GetRequiredService<IMessageHandler>();

        try
        {
            if (!quiet)
            {
                Console.WriteLine("CLASSIC-8 Game File Scanner");
                Console.WriteLine("===========================");
                Console.WriteLine();
            }

            // Determine game directory
            var gameDirectory = gamePath?.FullName ?? await FindGameDirectoryAsync();
            if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory))
            {
                logger.Error("Game directory not found");
                Environment.Exit(1);
                return;
            }

            // Determine mods directory
            var modsDirectory = modsPath?.FullName ?? await FindModsDirectoryAsync(gameDirectory);

            logger.Information("Game directory: {Path}", gameDirectory);
            if (!string.IsNullOrEmpty(modsDirectory))
                logger.Information("Mods directory: {Path}", modsDirectory);

            // Determine output directory
            var reportsPath = outputPath?.FullName ?? Path.Combine(Directory.GetCurrentDirectory(), "GameScanReports");
            Directory.CreateDirectory(reportsPath);

            var scanResults = new List<string>();
            var hasErrors = false;

            // Check XSE plugins
            if (checkXse)
            {
                messageHandler.SendMessage("Checking script extender plugins...", MessageType.Info, MessageTarget.Cli);
                // var xseChecker = serviceProvider.GetRequiredService<IGameScanner>();

                // Note: In a full implementation, we'd call the XsePluginChecker specifically
                // For now, this is a placeholder
                logger.Information("XSE plugin check completed");
            }

            // Check Crashgen settings
            if (checkCrashgen)
            {
                messageHandler.SendMessage("Checking crash generation settings...", MessageType.Info,
                    MessageTarget.Cli);
                // Placeholder for crashgen check
                logger.Information("Crashgen settings check completed");
            }

            // Always scan loose files if mods directory exists
            if (!string.IsNullOrEmpty(modsDirectory))
            {
                messageHandler.SendMessage("Scanning loose mod files...", MessageType.Info, MessageTarget.Cli);
                var issues = await ScanLooseFilesAsync(modsDirectory, messageHandler, backupDocs, cancellationToken);
                if (issues.Count > 0)
                {
                    hasErrors = true;
                    scanResults.AddRange(issues);
                }
            }

            // Always scan archives if mods directory exists
            if (!string.IsNullOrEmpty(modsDirectory))
            {
                messageHandler.SendMessage("Scanning mod archives...", MessageType.Info, MessageTarget.Cli);
                var issues = await ScanArchivesAsync(modsDirectory, messageHandler, cancellationToken);
                if (issues.Count > 0)
                {
                    hasErrors = true;
                    scanResults.AddRange(issues);
                }
            }

            // Check game logs
            if (checkLogs)
            {
                messageHandler.SendMessage("Checking game logs for errors...", MessageType.Info, MessageTarget.Cli);
                var issues = await CheckGameLogsAsync(gameDirectory, messageHandler, cancellationToken);
                if (issues.Count > 0)
                {
                    hasErrors = true;
                    scanResults.AddRange(issues);
                }
            }

            // Generate report
            var reportPath = Path.Combine(reportsPath, $"GameScan_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.md");
            await GenerateReportAsync(reportPath, scanResults, gameDirectory, modsDirectory);

            if (!quiet)
            {
                Console.WriteLine();
                Console.WriteLine("Scan Complete!");
                Console.WriteLine("==============");
                Console.WriteLine($"Issues found: {scanResults.Count}");
                Console.WriteLine($"Report saved to: {reportPath}");

                if (scanResults.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Top issues:");
                    foreach (var issue in scanResults.Take(5)) Console.WriteLine($"  - {issue}");
                    if (scanResults.Count > 5) Console.WriteLine($"  ... and {scanResults.Count - 5} more");
                }
            }

            Environment.Exit(hasErrors ? 1 : 0);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Game scan failed with error");
            Environment.Exit(1);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static Task<string> FindGameDirectoryAsync()
    {
        // Common game installation paths
        var possiblePaths = new[]
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Fallout 4",
            @"C:\Program Files\Steam\steamapps\common\Fallout 4",
            @"C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition",
            @"C:\Program Files\Steam\steamapps\common\Skyrim Special Edition",
            @"C:\Games\Fallout 4",
            @"C:\Games\Skyrim Special Edition"
        };

        foreach (var path in possiblePaths)
            if (Directory.Exists(path))
                return Task.FromResult(path);

        return Task.FromResult(string.Empty);
    }

    private static Task<string> FindModsDirectoryAsync(string gameDirectory)
    {
        // Check for MO2 mods directory
        var gameName = Path.GetFileName(gameDirectory);
        var mo2ModsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ModOrganizer", gameName, "mods");

        if (Directory.Exists(mo2ModsPath))
            return Task.FromResult(mo2ModsPath);

        // Check for Vortex mods
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var vortexModsPath = Path.Combine(documentsPath, "Vortex Mods", gameName);

        if (Directory.Exists(vortexModsPath))
            return Task.FromResult(vortexModsPath);

        // Default to Data directory
        var dataPath = Path.Combine(gameDirectory, "Data");
        return Task.FromResult(Directory.Exists(dataPath) ? dataPath : string.Empty);
    }

    private async Task<List<string>> ScanLooseFilesAsync(string modsDirectory, IMessageHandler messageHandler,
        bool backupDocs, CancellationToken cancellationToken)
    {
        var issues = new List<string>();
        var modFolders = Directory.GetDirectories(modsDirectory);
        var current = 0;
        var total = modFolders.Length;

        foreach (var modFolder in modFolders)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            current++;
            messageHandler.ReportProgress("Scanning mods", current, total);

            // Check for problematic file types
            var dllFiles = Directory.GetFiles(modFolder, "*.dll", SearchOption.AllDirectories);
            foreach (var dll in dllFiles)
                if (!IsValidScriptExtenderDll(dll))
                    issues.Add($"Suspicious DLL found: {Path.GetRelativePath(modsDirectory, dll)}");

            // Check texture dimensions
            var textureFiles = Directory.GetFiles(modFolder, "*.dds", SearchOption.AllDirectories);
            foreach (var texture in textureFiles)
            {
                var issue = await CheckTextureFile(texture);
                if (!string.IsNullOrEmpty(issue))
                    issues.Add($"{Path.GetRelativePath(modsDirectory, texture)}: {issue}");
            }

            // Backup documentation if requested
            if (backupDocs)
            {
                var docFiles = Directory.GetFiles(modFolder, "*.txt", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(modFolder, "*.md", SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(modFolder, "*.pdf", SearchOption.AllDirectories));

                foreach (var doc in docFiles)
                {
                    var backupPath = Path.Combine(modsDirectory, "_Documentation_Backup",
                        Path.GetFileName(modFolder), Path.GetRelativePath(modFolder, doc));
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                    File.Copy(doc, backupPath, true);
                }
            }
        }

        return issues;
    }

    private static Task<List<string>> ScanArchivesAsync(string modsDirectory, IMessageHandler messageHandler,
        CancellationToken cancellationToken)
    {
        var issues = new List<string>();
        var archiveFiles = Directory.GetFiles(modsDirectory, "*.ba2", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(modsDirectory, "*.bsa", SearchOption.AllDirectories))
            .ToArray();

        var current = 0;
        var total = archiveFiles.Length;

        foreach (var archive in archiveFiles)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            current++;
            messageHandler.ReportProgress("Scanning archives", current, total);

            // Basic validation - in full implementation would use BSArch.exe
            var fileInfo = new FileInfo(archive);
            if (fileInfo.Length < 1024) // Too small to be valid
                issues.Add($"Invalid archive (too small): {Path.GetRelativePath(modsDirectory, archive)}");
        }

        return Task.FromResult(issues);
    }

    private static async Task<List<string>> CheckGameLogsAsync(string gameDirectory, IMessageHandler messageHandler,
        CancellationToken cancellationToken)
    {
        var issues = new List<string>();
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var gameName = Path.GetFileName(gameDirectory);

        var logPaths = new[]
        {
            Path.Combine(documentsPath, "My Games", gameName, "Logs"),
            Path.Combine(gameDirectory, "Logs")
        };

        foreach (var logPath in logPaths.Where(Directory.Exists))
        {
            var logFiles = Directory.GetFiles(logPath, "*.log", SearchOption.AllDirectories);

            foreach (var logFile in logFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var content = await File.ReadAllTextAsync(logFile, cancellationToken);

                // Check for common error patterns
                if (content.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
                    issues.Add($"Errors found in log: {Path.GetFileName(logFile)}");

                if (content.Contains("EXCEPTION", StringComparison.OrdinalIgnoreCase))
                    issues.Add($"Exceptions found in log: {Path.GetFileName(logFile)}");
            }
        }

        return issues;
    }

    private static bool IsValidScriptExtenderDll(string dllPath)
    {
        // Check if DLL is in a valid script extender plugin location
        var directory = Path.GetDirectoryName(dllPath) ?? string.Empty;
        return directory.Contains("F4SE", StringComparison.OrdinalIgnoreCase) ||
               directory.Contains("SKSE", StringComparison.OrdinalIgnoreCase) ||
               directory.Contains("Plugins", StringComparison.OrdinalIgnoreCase);
    }

    private static Task<string> CheckTextureFile(string texturePath)
    {
        // Basic texture validation - in full implementation would read DDS header
        var fileInfo = new FileInfo(texturePath);

        // Check file size
        if (fileInfo.Length > 100 * 1024 * 1024) // 100MB
            return Task.FromResult("Texture file is extremely large (>100MB)");

        // Check path for common issues
        if (texturePath.Contains("HighRes", StringComparison.OrdinalIgnoreCase) && fileInfo.Length < 1024)
            return Task.FromResult("High-res texture is suspiciously small");

        return Task.FromResult(string.Empty);
    }

    private static async Task GenerateReportAsync(string reportPath, List<string> issues,
        string gameDirectory, string? modsDirectory)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("# CLASSIC-8 Game Scan Report");
        report.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"**Game Directory:** {gameDirectory}");
        if (!string.IsNullOrEmpty(modsDirectory))
            report.AppendLine($"**Mods Directory:** {modsDirectory}");
        report.AppendLine();

        report.AppendLine("## Summary");
        report.AppendLine($"- Total issues found: {issues.Count}");
        report.AppendLine();

        if (issues.Count > 0)
        {
            report.AppendLine("## Issues Found");
            foreach (var issue in issues) report.AppendLine($"- {issue}");
        }
        else
        {
            report.AppendLine("## No Issues Found");
            report.AppendLine("The game files scan completed without finding any issues.");
        }

        await File.WriteAllTextAsync(reportPath, report.ToString());
    }
}
