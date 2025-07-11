using Classic.Core.Enums;
using Classic.Core.Models;
using Classic.ScanLog.Configuration;
using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Classic.ScanLog.Reporting;

/// <summary>
/// Advanced report generator with comprehensive formatting and game-specific hints
/// </summary>
public class AdvancedReportGenerator
{
    private readonly GameHintsLoader _gameHintsLoader;
    private readonly ILogger<AdvancedReportGenerator> _logger;
    
    private const string DocumentationUrl = "https://docs.google.com/document/d/17FzeIMJ256xE85XdjoPvv_Zi3C5uHeSTQh6wOZugs4c";
    private const string CommunityUrl = "r/FalloutMods, Nexus Forums";

    public AdvancedReportGenerator(GameHintsLoader gameHintsLoader, ILogger<AdvancedReportGenerator> logger)
    {
        _gameHintsLoader = gameHintsLoader;
        _logger = logger;
    }

    /// <summary>
    /// Generates a comprehensive crash log analysis report
    /// </summary>
    public async Task<string> GenerateComprehensiveReportAsync(
        AdvancedReportData reportData, 
        CancellationToken cancellationToken = default)
    {
        var report = new StringBuilder();

        try
        {
            // Load configuration
            var hintsConfig = await _gameHintsLoader.LoadGameHintsAsync(cancellationToken);
            
            // Generate report sections
            await GenerateHeaderAsync(report, reportData, hintsConfig, cancellationToken);
            GenerateExecutiveSummary(report, reportData);
            GenerateErrorSection(report, reportData);
            await GenerateSuspectsSection(report, reportData, cancellationToken);
            GenerateModConflictsSection(report, reportData);
            GenerateFileValidationSection(report, reportData);
            GenerateSystemAnalysisSection(report, reportData);
            await GenerateRecommendationsSection(report, reportData, cancellationToken);
            GeneratePerformanceSection(report, reportData);
            await GenerateGameHintsSection(report, reportData, cancellationToken);
            await GenerateFooterAsync(report, reportData, hintsConfig, cancellationToken);

            _logger.LogInformation("Generated comprehensive report for {FileName} ({Length} chars)", 
                reportData.FileName, report.Length);

            return report.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate comprehensive report for {FileName}", reportData.FileName);
            return GenerateFallbackReport(reportData);
        }
    }

    /// <summary>
    /// Generates report header with instructions and metadata
    /// </summary>
    private async Task GenerateHeaderAsync(
        StringBuilder report, 
        AdvancedReportData reportData, 
        GameHintsConfig hintsConfig,
        CancellationToken cancellationToken)
    {
        var headerConfig = hintsConfig.ReportSections?.Header;
        
        report.AppendLine("╔════════════════════════════════════════════════════════════════════════════════╗");
        report.AppendLine($"║  {(headerConfig?.Title ?? "CLASSIC-8 Crash Log Analysis Report").PadRight(76)}  ║");
        report.AppendLine($"║  {(headerConfig?.Subtitle ?? "Comprehensive Analysis and Recommendations").PadRight(76)}  ║");
        report.AppendLine("╚════════════════════════════════════════════════════════════════════════════════╝");
        report.AppendLine();
        
        report.AppendLine($"📂 **File:** {reportData.FileName}");
        report.AppendLine($"🕐 **Generated:** {reportData.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"⚡ **Processing Time:** {reportData.ProcessingTime.TotalSeconds:F2}s");
        report.AppendLine($"🎮 **Game:** {GetGameDisplayName(reportData.GameInfo.GameId)}");
        report.AppendLine($"🔧 **CLASSIC Version:** {reportData.ClassicVersion}");
        report.AppendLine();
        
        // Instructions
        if (!string.IsNullOrEmpty(headerConfig?.Instructions))
        {
            report.AppendLine("📖 **How to read this report:**");
            report.AppendLine(headerConfig.Instructions);
        }
        else
        {
            report.AppendLine("📖 **How to read this report:**");
            report.AppendLine("• 🚨 Critical issues require immediate attention");
            report.AppendLine("• ⚠️ Warnings should be addressed for stability");
            report.AppendLine("• 💡 Recommendations can improve your experience");
            report.AppendLine("• 🔗 Links provide additional information and solutions");
        }
        
        report.AppendLine();
        report.AppendLine("═".PadRight(80, '═'));
        report.AppendLine();
    }

    /// <summary>
    /// Generates executive summary with key findings
    /// </summary>
    private void GenerateExecutiveSummary(StringBuilder report, AdvancedReportData reportData)
    {
        report.AppendLine("## 📊 Executive Summary");
        report.AppendLine();
        
        var totalIssues = reportData.CriticalIssues.Count + reportData.Errors.Count + reportData.Warnings.Count;
        var issuesSeverity = GetSeverityAssessment(reportData);
        
        report.AppendLine($"**Overall Assessment:** {issuesSeverity.Icon} {issuesSeverity.Description}");
        report.AppendLine($"**Total Issues Found:** {totalIssues}");
        report.AppendLine($"  • Critical: {reportData.CriticalIssues.Count}");
        report.AppendLine($"  • Errors: {reportData.Errors.Count}");
        report.AppendLine($"  • Warnings: {reportData.Warnings.Count}");
        report.AppendLine();
        
        // Key findings
        if (reportData.CrashSuspects.Any())
        {
            var topSuspect = reportData.CrashSuspects.OrderByDescending(s => s.SeverityScore).First();
            report.AppendLine($"**Primary Crash Suspect:** {topSuspect.Name} (Severity: {topSuspect.SeverityScore})");
        }
        
        if (reportData.ModConflicts.Any())
        {
            report.AppendLine($"**Mod Conflicts:** {reportData.ModConflicts.Count} detected");
        }
        
        if (reportData.FileValidationResults.Any(f => f.Status != ValidationStatus.Valid))
        {
            var fileIssues = reportData.FileValidationResults.Count(f => f.Status != ValidationStatus.Valid);
            report.AppendLine($"**File Validation Issues:** {fileIssues} files need attention");
        }
        
        report.AppendLine();
        report.AppendLine("─".PadRight(80, '─'));
        report.AppendLine();
    }

    /// <summary>
    /// Generates error section with main crash information
    /// </summary>
    private void GenerateErrorSection(StringBuilder report, AdvancedReportData reportData)
    {
        report.AppendLine("## 🚨 Crash Information");
        report.AppendLine();
        
        if (!string.IsNullOrEmpty(reportData.CrashLog.MainError))
        {
            report.AppendLine("**Main Error:**");
            report.AppendLine("```");
            report.AppendLine(reportData.CrashLog.MainError);
            report.AppendLine("```");
            report.AppendLine();
        }
        
        // Game and crash generator info
        report.AppendLine("**Environment Information:**");
        report.AppendLine($"• Game Version: {reportData.GameInfo.GameVersion}");
        report.AppendLine($"• {reportData.GameInfo.CrashGenName}: {reportData.GameInfo.CrashGenVersion}");
        
        if (reportData.GameInfo.IsOutdated)
        {
            report.AppendLine($"  ⚠️ **WARNING:** {reportData.GameInfo.CrashGenName} is outdated! Please update to {reportData.GameInfo.LatestVersion}");
        }
        else
        {
            report.AppendLine($"  ✅ You have the latest version of {reportData.GameInfo.CrashGenName}!");
        }
        
        report.AppendLine();
        report.AppendLine("─".PadRight(80, '─'));
        report.AppendLine();
    }

    /// <summary>
    /// Generates suspects section with detected crash causes
    /// </summary>
    private async Task GenerateSuspectsSection(StringBuilder report, AdvancedReportData reportData, CancellationToken cancellationToken)
    {
        report.AppendLine("## 🔍 Crash Suspects Analysis");
        report.AppendLine();
        
        if (!reportData.CrashSuspects.Any())
        {
            report.AppendLine("🔍 **No specific crash suspects identified in the database.**");
            report.AppendLine();
            report.AppendLine("This could mean:");
            report.AppendLine("• The crash is caused by a rare or unknown issue");
            report.AppendLine("• The crash log format is unusual");
            report.AppendLine("• Manual analysis of the call stack may be needed");
            report.AppendLine();
        }
        else
        {
            report.AppendLine($"**Found {reportData.CrashSuspects.Count} potential crash suspects:**");
            report.AppendLine();
            
            foreach (var suspect in reportData.CrashSuspects.OrderByDescending(s => s.SeverityScore))
            {
                var severityIcon = GetSeverityIcon(suspect.SeverityScore);
                report.AppendLine($"### {severityIcon} {suspect.Name}");
                report.AppendLine($"**Severity:** {suspect.SeverityScore}/5");
                
                if (!string.IsNullOrEmpty(suspect.Description))
                {
                    report.AppendLine($"**Description:** {suspect.Description}");
                }
                
                if (!string.IsNullOrEmpty(suspect.Evidence))
                {
                    report.AppendLine($"**Evidence:** {suspect.Evidence}");
                }
                
                if (!string.IsNullOrEmpty(suspect.Recommendation))
                {
                    report.AppendLine($"**Recommendation:** {suspect.Recommendation}");
                }
                
                if (suspect.RelatedFiles.Any())
                {
                    report.AppendLine($"**Related Files:** {string.Join(", ", suspect.RelatedFiles)}");
                }
                
                report.AppendLine();
            }
            
            report.AppendLine($"📖 **For detailed descriptions and solutions, see:** {DocumentationUrl}");
        }
        
        report.AppendLine();
        report.AppendLine("─".PadRight(80, '─'));
        report.AppendLine();
    }

    /// <summary>
    /// Generates mod conflicts section
    /// </summary>
    private void GenerateModConflictsSection(StringBuilder report, AdvancedReportData reportData)
    {
        if (!reportData.ModConflicts.Any())
            return;
            
        report.AppendLine("## ⚔️ Mod Conflicts");
        report.AppendLine();
        
        var groupedConflicts = reportData.ModConflicts.GroupBy(m => m.Type);
        
        foreach (var group in groupedConflicts.OrderBy(g => g.Key))
        {
            var typeIcon = GetModConflictTypeIcon(group.Key);
            report.AppendLine($"### {typeIcon} {GetModConflictTypeDisplayName(group.Key)}");
            report.AppendLine();
            
            foreach (var conflict in group.OrderBy(c => c.ModName))
            {
                report.AppendLine($"• **{conflict.ModName}**");
                if (!string.IsNullOrEmpty(conflict.Warning))
                {
                    report.AppendLine($"  {conflict.Warning}");
                }
                if (!string.IsNullOrEmpty(conflict.Solution))
                {
                    report.AppendLine($"  **Solution:** {conflict.Solution}");
                }
                report.AppendLine();
            }
        }
        
        report.AppendLine("─".PadRight(80, '─'));
        report.AppendLine();
    }

    /// <summary>
    /// Generates file validation section
    /// </summary>
    private void GenerateFileValidationSection(StringBuilder report, AdvancedReportData reportData)
    {
        var fileIssues = reportData.FileValidationResults.Where(f => f.Status != ValidationStatus.Valid).ToList();
        
        if (!fileIssues.Any())
            return;
            
        report.AppendLine("## 📁 File Validation Issues");
        report.AppendLine();
        
        var groupedByType = fileIssues.GroupBy(f => f.ValidationType);
        
        foreach (var group in groupedByType)
        {
            var typeIcon = GetFileValidationTypeIcon(group.Key);
            report.AppendLine($"### {typeIcon} {group.Key} Files");
            report.AppendLine();
            
            foreach (var issue in group.OrderByDescending(i => i.Status))
            {
                var statusIcon = GetValidationStatusIcon(issue.Status);
                report.AppendLine($"{statusIcon} **{issue.RelativePath}**");
                
                if (!string.IsNullOrEmpty(issue.Issue))
                {
                    report.AppendLine($"  Issue: {issue.Issue}");
                }
                
                if (!string.IsNullOrEmpty(issue.Description))
                {
                    report.AppendLine($"  Description: {issue.Description}");
                }
                
                if (!string.IsNullOrEmpty(issue.Recommendation))
                {
                    report.AppendLine($"  **Recommendation:** {issue.Recommendation}");
                }
                
                report.AppendLine();
            }
        }
        
        report.AppendLine("─".PadRight(80, '─'));
        report.AppendLine();
    }

    /// <summary>
    /// Generates system analysis section
    /// </summary>
    private void GenerateSystemAnalysisSection(StringBuilder report, AdvancedReportData reportData)
    {
        report.AppendLine("## 💻 System Analysis");
        report.AppendLine();
        
        report.AppendLine("**System Specifications:**");
        report.AppendLine($"• **OS:** {reportData.SystemInfo.OperatingSystem}");
        report.AppendLine($"• **CPU:** {reportData.SystemInfo.Cpu}");
        report.AppendLine($"• **GPU:** {reportData.SystemInfo.Gpu}");
        report.AppendLine($"• **Memory:** {reportData.SystemInfo.Memory}");
        report.AppendLine();
        
        // GPU-specific analysis
        if (reportData.SystemInfo.GpuManufacturer != GpuManufacturer.Unknown)
        {
            report.AppendLine($"**GPU Analysis:** Detected {reportData.SystemInfo.GpuManufacturer} GPU");
            
            // Add GPU-specific recommendations here if needed
            report.AppendLine();
        }
        
        report.AppendLine("─".PadRight(80, '─'));
        report.AppendLine();
    }

    /// <summary>
    /// Generates recommendations section
    /// </summary>
    private async Task GenerateRecommendationsSection(StringBuilder report, AdvancedReportData reportData, CancellationToken cancellationToken)
    {
        report.AppendLine("## 💡 Recommendations");
        report.AppendLine();
        
        if (reportData.Recommendations.Any())
        {
            foreach (var recommendation in reportData.Recommendations.OrderByDescending(r => r.Severity))
            {
                var icon = GetRecommendationIcon(recommendation.Type);
                report.AppendLine($"{icon} **{recommendation.Title}**");
                report.AppendLine($"  {recommendation.Description}");
                
                if (!string.IsNullOrEmpty(recommendation.DocumentationUrl))
                {
                    report.AppendLine($"  📖 More info: {recommendation.DocumentationUrl}");
                }
                
                report.AppendLine();
            }
        }
        
        // GPU-specific recommendations
        if (reportData.SystemInfo.GpuManufacturer != GpuManufacturer.Unknown)
        {
            var gpuRecommendations = await _gameHintsLoader.GetGpuRecommendationsAsync(
                reportData.SystemInfo.GpuManufacturer, cancellationToken);
                
            if (gpuRecommendations.Any())
            {
                report.AppendLine($"**{reportData.SystemInfo.GpuManufacturer} GPU Recommendations:**");
                foreach (var rec in gpuRecommendations)
                {
                    report.AppendLine($"• {rec}");
                }
                report.AppendLine();
            }
        }
        
        report.AppendLine("─".PadRight(80, '─'));
        report.AppendLine();
    }

    /// <summary>
    /// Generates performance analysis section
    /// </summary>
    private void GeneratePerformanceSection(StringBuilder report, AdvancedReportData reportData)
    {
        report.AppendLine("## ⚡ Performance Analysis");
        report.AppendLine();
        
        var scanTime = reportData.ProcessingTime.TotalSeconds;
        var performanceAssessment = GetPerformanceAssessment(scanTime);
        
        report.AppendLine($"**Scan Performance:** {performanceAssessment.Icon} {performanceAssessment.Description}");
        report.AppendLine($"**Processing Time:** {scanTime:F2} seconds");
        
        if (reportData.Performance.WorkerThreadsUsed > 0)
        {
            report.AppendLine($"**Worker Threads:** {reportData.Performance.WorkerThreadsUsed}");
            report.AppendLine($"**Files Processed:** {reportData.Performance.TotalFilesProcessed:N0}");
        }
        
        report.AppendLine();
        report.AppendLine("─".PadRight(80, '─'));
        report.AppendLine();
    }

    /// <summary>
    /// Generates game hints section
    /// </summary>
    private async Task GenerateGameHintsSection(StringBuilder report, AdvancedReportData reportData, CancellationToken cancellationToken)
    {
        var hints = await _gameHintsLoader.GetRandomHintsAsync(3, cancellationToken);
        
        if (!hints.Any())
            return;
            
        report.AppendLine("## 💡 Game Tips & Hints");
        report.AppendLine();
        
        foreach (var hint in hints)
        {
            report.AppendLine($"💡 {hint}");
            report.AppendLine();
        }
        
        report.AppendLine("─".PadRight(80, '─'));
        report.AppendLine();
    }

    /// <summary>
    /// Generates footer with metadata and links
    /// </summary>
    private async Task GenerateFooterAsync(
        StringBuilder report, 
        AdvancedReportData reportData, 
        GameHintsConfig hintsConfig,
        CancellationToken cancellationToken)
    {
        var footerConfig = hintsConfig.ReportSections?.Footer;
        
        report.AppendLine("═".PadRight(80, '═'));
        report.AppendLine();
        
        var generatedBy = footerConfig?.GeneratedBy?.Replace("{version}", reportData.ClassicVersion) 
                         ?? $"Generated by CLASSIC-8 v{reportData.ClassicVersion}";
        report.AppendLine($"🔧 {generatedBy}");
        
        var documentation = footerConfig?.Documentation ?? DocumentationUrl;
        report.AppendLine($"📖 Documentation: {documentation}");
        
        var community = footerConfig?.Community ?? CommunityUrl;
        report.AppendLine($"👥 Community: {community}");
        
        report.AppendLine($"📅 Report generated: {reportData.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        
        report.AppendLine("Thank you for using CLASSIC-8! 🎮");
    }

    // Helper methods for formatting and icons

    private string GetGameDisplayName(GameId gameId) => gameId switch
    {
        GameId.Fallout4 => "Fallout 4",
        GameId.SkyrimSE => "Skyrim Special Edition",
        GameId.SkyrimVR => "Skyrim VR",
        GameId.Fallout4VR => "Fallout 4 VR",
        _ => "Unknown Game"
    };

    private (string Icon, string Description) GetSeverityAssessment(AdvancedReportData reportData)
    {
        if (reportData.CriticalIssues.Any())
            return ("🚨", "Critical issues detected - immediate action required");
        if (reportData.Errors.Any())
            return ("⚠️", "Errors found - should be addressed for stability");
        if (reportData.Warnings.Any())
            return ("ℹ️", "Minor warnings found - consider addressing");
        return ("✅", "No major issues detected");
    }

    private string GetSeverityIcon(int severity) => severity switch
    {
        >= 5 => "🚨",
        4 => "⚠️",
        3 => "⚠️",
        2 => "ℹ️",
        _ => "📝"
    };

    private string GetModConflictTypeIcon(ConflictType type) => type switch
    {
        ConflictType.FrequentCrash => "💥",
        ConflictType.ModPairConflict => "⚔️",
        ConflictType.MissingImportant => "❓",
        ConflictType.HasSolution => "🔧",
        ConflictType.GpuIncompatible => "🖥️",
        ConflictType.LoadOrderIssue => "📋",
        _ => "⚠️"
    };

    private string GetModConflictTypeDisplayName(ConflictType type) => type switch
    {
        ConflictType.FrequentCrash => "Frequent Crash Mods",
        ConflictType.ModPairConflict => "Mod Conflicts",
        ConflictType.MissingImportant => "Missing Important Mods",
        ConflictType.HasSolution => "Mods with Solutions",
        ConflictType.GpuIncompatible => "GPU Incompatible Mods",
        ConflictType.LoadOrderIssue => "Load Order Issues",
        _ => type.ToString()
    };

    private string GetFileValidationTypeIcon(FileValidationType type) => type switch
    {
        FileValidationType.Texture => "🎨",
        FileValidationType.Archive => "📦",
        FileValidationType.Audio => "🔊",
        FileValidationType.Script => "📜",
        FileValidationType.Configuration => "⚙️",
        FileValidationType.Previs => "🏗️",
        _ => "📁"
    };

    private string GetValidationStatusIcon(ValidationStatus status) => status switch
    {
        ValidationStatus.Critical => "🚨",
        ValidationStatus.Error => "❌",
        ValidationStatus.Warning => "⚠️",
        ValidationStatus.Valid => "✅",
        _ => "❓"
    };

    private string GetRecommendationIcon(ReportIssueType type) => type switch
    {
        ReportIssueType.ModConflict => "⚔️",
        ReportIssueType.PerformanceIssue => "⚡",
        ReportIssueType.SystemCompatibility => "💻",
        ReportIssueType.GameConfiguration => "⚙️",
        _ => "💡"
    };

    private (string Icon, string Description) GetPerformanceAssessment(double scanTimeSeconds) => scanTimeSeconds switch
    {
        <= 2.0 => ("🚀", "Excellent"),
        <= 5.0 => ("✅", "Good"),
        <= 10.0 => ("⚠️", "Fair"),
        _ => ("🐌", "Slow")
    };

    /// <summary>
    /// Generates a basic fallback report if the comprehensive generation fails
    /// </summary>
    private string GenerateFallbackReport(AdvancedReportData reportData)
    {
        var report = new StringBuilder();
        
        report.AppendLine("# CLASSIC-8 Crash Log Analysis Report");
        report.AppendLine("(Fallback Mode - Limited Information Available)");
        report.AppendLine();
        report.AppendLine($"**File:** {reportData.FileName}");
        report.AppendLine($"**Generated:** {reportData.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        
        if (!string.IsNullOrEmpty(reportData.CrashLog.MainError))
        {
            report.AppendLine("**Main Error:**");
            report.AppendLine(reportData.CrashLog.MainError);
            report.AppendLine();
        }
        
        report.AppendLine("⚠️ An error occurred during comprehensive report generation.");
        report.AppendLine("Please check the logs for more information.");
        
        return report.ToString();
    }
}