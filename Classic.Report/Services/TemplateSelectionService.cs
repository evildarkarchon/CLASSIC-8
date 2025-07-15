using Classic.Core.Models;
using Classic.Report.Models;
using Serilog;

namespace Classic.Report.Services;

/// <summary>
/// Service for making intelligent template selection decisions.
/// </summary>
public class TemplateSelectionService
{
    private readonly ILogger _logger;

    public TemplateSelectionService(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Determines the optimal report format based on analysis results and options.
    /// </summary>
    public ReportTemplateType DetermineOptimalFormat(
        CrashLogAnalysisResult analysisResult,
        ReportOptions options)
    {
        _logger.Debug("Determining optimal report format for {FileName}",
            analysisResult.CrashLog.FileName);

        // FCX mode always uses Advanced
        if (options.FCXMode)
        {
            _logger.Debug("FCX mode enabled - selecting Advanced format");
            return ReportTemplateType.Advanced;
        }

        // Explicit format preference (when auto-select is disabled)
        if (!options.AutoSelectFormat)
        {
            _logger.Debug("Auto-select disabled - using preferred format: {Format}",
                options.PreferredFormat);
            return options.PreferredFormat;
        }

        // Enhanced format explicitly requested
        if (options.UseEnhancedFormatting)
        {
            _logger.Debug("Enhanced formatting explicitly requested");
            return ReportTemplateType.Enhanced;
        }

        // Auto-selection based on complexity
        var complexity = AnalyzeCrashComplexity(analysisResult);

        if (complexity.ShouldUseEnhanced)
        {
            _logger.Information("Auto-selected Enhanced format due to crash complexity: {Reason}",
                complexity.Reason);
            return ReportTemplateType.Enhanced;
        }

        _logger.Debug("Selected Standard format - basic complexity detected");
        return ReportTemplateType.Standard;
    }

    /// <summary>
    /// Analyzes the complexity of a crash to determine if enhanced formatting would be beneficial.
    /// </summary>
    public CrashComplexityAnalysis AnalyzeCrashComplexity(CrashLogAnalysisResult analysisResult)
    {
        var analysis = new CrashComplexityAnalysis();
        var reasons = new List<string>();

        // Check various complexity indicators
        if (analysisResult.CrashSuspects.Count >= 5)
        {
            analysis.ComplexityScore += 2;
            reasons.Add($"High suspect count ({analysisResult.CrashSuspects.Count})");
        }

        if (analysisResult.ModCompatibilityIssues.Count >= 3)
        {
            analysis.ComplexityScore += 2;
            reasons.Add($"Multiple mod conflicts ({analysisResult.ModCompatibilityIssues.Count})");
        }

        if (analysisResult.ProblematicPlugins.Count >= 10)
        {
            analysis.ComplexityScore += 1;
            reasons.Add($"Many problematic plugins ({analysisResult.ProblematicPlugins.Count})");
        }

        var highSeverityIssues = analysisResult.SettingsValidation.Issues.Count(i => i.SeverityScore >= 4);
        if (highSeverityIssues >= 1)
        {
            analysis.ComplexityScore += 2;
            reasons.Add($"High severity setting issues ({highSeverityIssues})");
        }

        if (analysisResult.SuspectFormIds.Count >= 20)
        {
            analysis.ComplexityScore += 1;
            reasons.Add($"Many suspect FormIDs ({analysisResult.SuspectFormIds.Count})");
        }

        if (analysisResult.NamedRecords.Count >= 50)
        {
            analysis.ComplexityScore += 1;
            reasons.Add($"Large number of named records ({analysisResult.NamedRecords.Count})");
        }

        // Check for specific crash types that benefit from enhanced formatting
        if (IsCriticalCrash(analysisResult))
        {
            analysis.ComplexityScore += 3;
            reasons.Add("Critical crash type detected");
        }

        analysis.ShouldUseEnhanced = analysis.ComplexityScore >= 3;
        analysis.Reason = reasons.Any() ? string.Join(", ", reasons) : "Standard complexity";

        _logger.Debug("Complexity analysis: Score={Score}, Enhanced={ShouldUse}, Reason={Reason}",
            analysis.ComplexityScore, analysis.ShouldUseEnhanced, analysis.Reason);

        return analysis;
    }

    private bool IsCriticalCrash(CrashLogAnalysisResult analysisResult)
    {
        // Define critical crash patterns that benefit from detailed analysis
        var criticalPatterns = new[]
        {
            "access violation",
            "stack overflow",
            "heap corruption",
            "memory allocation",
            "infinite loop",
            "script engine",
            "papyrus"
        };

        var errorText = $"{analysisResult.CrashLog.MainError} {analysisResult.CrashLog.MainError}".ToLowerInvariant();

        return criticalPatterns.Any(pattern => errorText.Contains(pattern));
    }
}

/// <summary>
/// Result of crash complexity analysis.
/// </summary>
public class CrashComplexityAnalysis
{
    /// <summary>
    /// Numerical score representing the complexity of the crash.
    /// </summary>
    public int ComplexityScore { get; set; } = 0;

    /// <summary>
    /// Whether enhanced formatting should be used based on the complexity.
    /// </summary>
    public bool ShouldUseEnhanced { get; set; } = false;

    /// <summary>
    /// Human-readable reason for the complexity determination.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
