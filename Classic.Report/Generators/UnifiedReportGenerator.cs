using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.Report.Interfaces;
using Classic.Report.Models;
using Serilog;

namespace Classic.Report.Generators;

/// <summary>
/// Unified report generator that implements the three-tier template system.
/// </summary>
public class UnifiedReportGenerator : IUnifiedReportGenerator
{
    private readonly IReportTemplate _template;
    private readonly IReportStrategyFactory _strategyFactory;
    private readonly ILogger _logger;

    public UnifiedReportGenerator(
        IReportTemplate template,
        IReportStrategyFactory strategyFactory,
        ILogger logger)
    {
        _template = template;
        _strategyFactory = strategyFactory;
        _logger = logger;
    }

    public async Task<string> GenerateReportAsync(
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting unified report generation for {FileName}",
            analysisResult.CrashLog.FileName);

        try
        {
            // Select the appropriate strategy
            var strategy = SelectStrategy(options, analysisResult);

            _logger.Information("Selected {Strategy} for report generation", strategy.Name);

            // Generate sections using the selected strategy
            var sections = await strategy.GenerateSectionsAsync(analysisResult, options, cancellationToken)
                .ConfigureAwait(false);

            // Apply template formatting
            var report = await ApplyTemplateAsync(sections, strategy.Format, cancellationToken)
                .ConfigureAwait(false);

            _logger.Information("Successfully generated {Strategy} report with {Length} characters",
                strategy.Name, report.Length);

            return report;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate unified report for {FileName}",
                analysisResult.CrashLog.FileName);
            throw;
        }
    }

    public async Task<ReportSections> GenerateSectionsAsync(
        CrashLogAnalysisResult analysisResult,
        ReportOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Generating report sections only for {FileName}",
            analysisResult.CrashLog.FileName);

        var strategy = SelectStrategy(options, analysisResult);
        return await strategy.GenerateSectionsAsync(analysisResult, options, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<string> ApplyTemplateAsync(
        ReportSections sections,
        ReportTemplateType format,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Applying {Format} template formatting", format);

        try
        {
            var report = await FormatSectionsAsync(sections, format).ConfigureAwait(false);

            _logger.Debug("Successfully applied template formatting, result length: {Length}",
                report.Length);

            return report;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply template formatting for {Format}", format);
            throw;
        }
    }

    private IReportStrategy SelectStrategy(ReportOptions options, CrashLogAnalysisResult analysisResult)
    {
        // Decision tree for strategy selection
        if (options.FCXMode)
        {
            _logger.Debug("FCX mode enabled, selecting Advanced strategy");
            return _strategyFactory.GetStrategy(ReportTemplateType.Advanced);
        }

        // Check if enhanced format is explicitly requested
        if (options.UseEnhancedFormatting)
        {
            _logger.Debug("Enhanced formatting requested, selecting Enhanced strategy");
            return _strategyFactory.GetStrategy(ReportTemplateType.Enhanced);
        }

        // Auto-select based on preferred format if not auto
        if (!options.AutoSelectFormat)
        {
            _logger.Debug("Auto-select disabled, using preferred format: {Format}", options.PreferredFormat);
            return _strategyFactory.GetStrategy(options.PreferredFormat);
        }

        // Auto-select based on analysis complexity
        if (ShouldUseEnhancedFormat(analysisResult))
        {
            _logger.Debug("Complex crash detected, auto-selecting Enhanced strategy");
            return _strategyFactory.GetStrategy(ReportTemplateType.Enhanced);
        }

        _logger.Debug("Standard conditions met, selecting Standard strategy");
        return _strategyFactory.GetStrategy(ReportTemplateType.Standard);
    }

    private bool ShouldUseEnhancedFormat(CrashLogAnalysisResult result)
    {
        // Use enhanced format for complex crashes
        var shouldUseEnhanced = result.CrashSuspects.Count >= 5 ||
                                result.ModCompatibilityIssues.Count >= 3 ||
                                result.SettingsValidation.Issues.Any(i => i.SeverityScore >= 4) ||
                                result.ProblematicPlugins.Count >= 10;

        if (shouldUseEnhanced)
        {
            _logger.Debug("Enhanced format criteria met: Suspects={SuspectCount}, " +
                          "Conflicts={ConflictCount}, HighSeverityIssues={HighSeverityCount}, " +
                          "ProblematicPlugins={PluginCount}",
                result.CrashSuspects.Count,
                result.ModCompatibilityIssues.Count,
                result.SettingsValidation.Issues.Count(i => i.SeverityScore >= 4),
                result.ProblematicPlugins.Count);
        }

        return shouldUseEnhanced;
    }

    private async Task<string> FormatSectionsAsync(ReportSections sections, ReportTemplateType format)
    {
        var reportParts = new List<string>();

        // Header
        if (sections.Header != null)
        {
            reportParts.Add(_template.FormatHeader(
                sections.Header.FileName,
                sections.Header.Version));

            if (sections.Header.FCXModeEnabled)
            {
                reportParts.Add("**FCX Mode Enabled** - Extended file validation and performance metrics included.");
                reportParts.Add(_template.FormatSeparator());
            }
        }

        // FCX Notice (Advanced only)
        if (format == ReportTemplateType.Advanced && sections.FCXNotice != null)
        {
            reportParts.Add(_template.FormatSectionHeader(sections.FCXNotice.Title));
            reportParts.Add(sections.FCXNotice.NoticeText);
            reportParts.Add(_template.FormatSeparator());
        }

        // Executive Summary (Enhanced/Advanced)
        if ((format == ReportTemplateType.Enhanced || format == ReportTemplateType.Advanced)
            && sections.ExecutiveSummary != null)
        {
            reportParts.Add(_template.FormatSectionHeader(sections.ExecutiveSummary.Title));
            reportParts.Add(sections.ExecutiveSummary.SummaryText);

            if (!string.IsNullOrEmpty(sections.ExecutiveSummary.RecommendedActions))
            {
                reportParts.Add("**Recommended Actions:**");
                reportParts.Add(sections.ExecutiveSummary.RecommendedActions);
            }

            reportParts.Add(_template.FormatSeparator());
        }

        // Main Error
        if (sections.MainError != null)
        {
            reportParts.Add(_template.FormatSectionHeader("Crash Analysis"));
            reportParts.Add(_template.FormatError(
                sections.MainError.ErrorType,
                sections.MainError.ErrorMessage,
                sections.MainError.SeverityScore));
            reportParts.Add(_template.FormatSeparator());
        }

        // Crash Suspects
        if (sections.CrashSuspects.Any())
        {
            reportParts.Add(_template.FormatSectionHeader("Crash Suspects"));

            foreach (var suspect in sections.CrashSuspects)
            {
                reportParts.Add(_template.FormatSuspect(
                    suspect.Name,
                    suspect.Description,
                    suspect.SeverityScore,
                    suspect.Evidence,
                    suspect.Recommendation));
            }

            reportParts.Add(_template.FormatSeparator());
        }

        // Settings Validation
        if (sections.Settings != null)
        {
            reportParts.Add(_template.FormatSectionHeader("Settings Validation"));

            if (sections.Settings.AllSettingsValid)
            {
                reportParts.Add("✅ All settings appear to be configured correctly.");
            }
            else
            {
                reportParts.Add($"⚠️ Found {sections.Settings.Issues.Count} setting issues:");

                foreach (var issue in sections.Settings.Issues)
                {
                    reportParts.Add($"• **{issue.SettingName}**: {issue.IssueDescription}");
                    if (!string.IsNullOrEmpty(issue.RecommendedValue))
                    {
                        reportParts.Add($"  Recommended: `{issue.RecommendedValue}`");
                    }
                }
            }

            reportParts.Add(_template.FormatSeparator());
        }

        // Plugin Suspects
        if (sections.PluginSuspects.Any())
        {
            reportParts.Add(_template.FormatSectionHeader("Plugin Analysis"));

            foreach (var plugin in sections.PluginSuspects)
            {
                reportParts.Add(_template.FormatPlugin(
                    plugin.LoadOrder,
                    plugin.Name,
                    plugin.Status,
                    plugin.Flags));
            }

            reportParts.Add(_template.FormatSeparator());
        }

        // FormID Analysis
        if (sections.FormIdSuspects.Any())
        {
            reportParts.Add(_template.FormatSectionHeader("FormID Analysis"));

            foreach (var formId in sections.FormIdSuspects)
            {
                reportParts.Add(_template.FormatFormId(
                    formId.FormId,
                    formId.PluginIndex,
                    formId.LocalFormId,
                    formId.PluginName,
                    formId.FormType));
            }

            reportParts.Add(_template.FormatSeparator());
        }

        // Named Records
        if (sections.NamedRecords.Any())
        {
            reportParts.Add(_template.FormatSectionHeader("Named Records"));

            foreach (var record in sections.NamedRecords)
            {
                reportParts.Add($"• **{record.RecordName}** ({record.RecordType}) - {record.PluginName}");
                reportParts.Add($"  FormID: 0x{record.FormId:X8}");
            }

            reportParts.Add(_template.FormatSeparator());
        }

        // Performance Metrics (Enhanced/Advanced)
        if ((format == ReportTemplateType.Enhanced || format == ReportTemplateType.Advanced)
            && sections.Performance != null)
        {
            reportParts.Add(_template.FormatSectionHeader("Performance Metrics"));
            reportParts.Add($"• Analysis Time: {sections.Performance.AnalysisTime.TotalMilliseconds:F0}ms");
            reportParts.Add($"• Files Processed: {sections.Performance.FilesProcessed}");
            reportParts.Add($"• Plugins Analyzed: {sections.Performance.PluginsAnalyzed}");
            reportParts.Add($"• Memory Used: {sections.Performance.MemoryUsed / 1024 / 1024:F1} MB");
            reportParts.Add(_template.FormatSeparator());
        }

        // Game Hints (Enhanced/Advanced)
        if ((format == ReportTemplateType.Enhanced || format == ReportTemplateType.Advanced)
            && sections.GameHints?.Hints.Any() == true)
        {
            reportParts.Add(_template.FormatSectionHeader("Game-Specific Hints"));

            foreach (var hint in sections.GameHints.Hints.OrderBy(h => h.Priority))
            {
                reportParts.Add($"• **{hint.Category}**: {hint.HintText}");
                if (!string.IsNullOrEmpty(hint.RelatedPlugin))
                {
                    reportParts.Add($"  Related to: {hint.RelatedPlugin}");
                }
            }

            reportParts.Add(_template.FormatSeparator());
        }

        // FCX File Checks (Advanced only)
        if (format == ReportTemplateType.Advanced)
        {
            await AddFCXSectionsAsync(reportParts, sections).ConfigureAwait(false);
        }

        // Footer
        if (sections.Footer != null)
        {
            reportParts.Add(_template.FormatFooter(
                sections.Footer.GeneratorVersion,
                sections.Footer.GenerationDate));
        }

        return string.Join(Environment.NewLine, reportParts);
    }

    private async Task AddFCXSectionsAsync(List<string> reportParts, ReportSections sections)
    {
        // Main Files Check
        if (sections.MainFilesCheck != null)
        {
            reportParts.Add(_template.FormatSectionHeader(sections.MainFilesCheck.Title));

            if (sections.MainFilesCheck.AllFilesValid)
            {
                reportParts.Add("✅ All main game files validated successfully.");
            }
            else
            {
                reportParts.Add("⚠️ Issues found with main game files:");
                foreach (var fileResult in sections.MainFilesCheck.FileResults.Where(r => !r.IsValid))
                {
                    reportParts.Add($"• **{Path.GetFileName(fileResult.FilePath)}**: {fileResult.IssueDescription}");
                }
            }

            reportParts.Add(_template.FormatSeparator());
        }

        // Game Files Check
        if (sections.GameFilesCheck != null)
        {
            reportParts.Add(_template.FormatSectionHeader(sections.GameFilesCheck.Title));

            reportParts.Add($"Files Checked: {sections.GameFilesCheck.TotalFilesChecked}");

            if (sections.GameFilesCheck.IntegrityCheckPassed)
            {
                reportParts.Add("✅ Game files integrity check passed.");
            }
            else
            {
                reportParts.Add("⚠️ Game files integrity issues detected:");
                foreach (var fileResult in sections.GameFilesCheck.FileResults.Where(r => !r.IsValid))
                {
                    reportParts.Add($"• **{Path.GetFileName(fileResult.FilePath)}**: {fileResult.IssueDescription}");
                }
            }

            reportParts.Add(_template.FormatSeparator());
        }

        // Extended Performance
        if (sections.ExtendedPerformance != null)
        {
            reportParts.Add(_template.FormatSectionHeader("Extended Performance Metrics"));
            reportParts.Add(
                $"• File System Check Time: {sections.ExtendedPerformance.FileSystemCheckTime.TotalMilliseconds:F0}ms");
            reportParts.Add($"• Worker Threads Used: {sections.ExtendedPerformance.WorkerThreadsUsed}");
            reportParts.Add($"• I/O Operations: {sections.ExtendedPerformance.IOOperations:N0}");
            reportParts.Add(
                $"• Total Processing Time: {sections.ExtendedPerformance.TotalProcessingTime.TotalSeconds:F1}s");
            reportParts.Add(_template.FormatSeparator());
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
