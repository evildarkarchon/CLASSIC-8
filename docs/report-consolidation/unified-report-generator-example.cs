using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Reporting
{
    /// <summary>
    /// Unified report generator that handles both standard and FCX mode reports
    /// </summary>
    public class UnifiedReportGenerator : IReportGenerator
    {
        private readonly IReportTemplate _template;
        private readonly IReportStrategyFactory _strategyFactory;
        private readonly IEnumerable<ISectionGenerator> _sectionGenerators;
        private readonly ILogger<UnifiedReportGenerator> _logger;

        public UnifiedReportGenerator(
            IReportTemplate template,
            IReportStrategyFactory strategyFactory,
            IEnumerable<ISectionGenerator> sectionGenerators,
            ILogger<UnifiedReportGenerator> logger)
        {
            _template = template;
            _strategyFactory = strategyFactory;
            _sectionGenerators = sectionGenerators;
            _logger = logger;
        }

        public async Task<string> GenerateReportAsync(
            CrashLogAnalysisResult analysisResult,
            ReportOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating {Mode} report for {FileName}",
                    options.FCXMode ? "FCX" : "Standard",
                    analysisResult.CrashLog.FileName);

                // Select appropriate strategy based on FCX mode
                var strategy = _strategyFactory.GetStrategy(options.FCXMode);
                
                // Generate report sections using the strategy
                var sections = await strategy.GenerateSectionsAsync(
                    analysisResult, 
                    _sectionGenerators,
                    cancellationToken);

                // Build the final report
                var report = new StringBuilder();
                
                // Apply template formatting to each section
                await ApplySectionsToReportAsync(sections, report, cancellationToken);

                _logger.LogInformation("Successfully generated report ({Length} chars)", 
                    report.Length);

                return report.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report for {FileName}", 
                    analysisResult.CrashLog.FileName);
                return GenerateFallbackReport(analysisResult);
            }
        }

        private async Task ApplySectionsToReportAsync(
            ReportSections sections,
            StringBuilder report,
            CancellationToken cancellationToken)
        {
            // Header
            if (sections.Header != null)
            {
                report.AppendLine(_template.FormatHeader(
                    sections.Header.FileName, 
                    sections.Header.Version));
                
                // Add FCX mode notice if enabled
                if (sections.FCXNotice != null)
                {
                    report.AppendLine(FormatFCXNotice(sections.FCXNotice));
                }
            }

            // Main Error Section
            if (sections.MainError != null)
            {
                report.AppendLine(_template.FormatSectionHeader("ERROR ANALYSIS"));
                report.AppendLine(FormatErrorSection(sections.MainError));
            }

            // Settings Validation
            if (sections.Settings?.Issues.Any() == true)
            {
                report.AppendLine(_template.FormatSectionHeader(
                    "CHECKING IF NECESSARY FILES/SETTINGS ARE CORRECT..."));
                FormatSettingsSection(sections.Settings, report);
            }

            // FCX Mode Specific: Main Files Check
            if (sections.MainFilesCheck != null)
            {
                report.AppendLine(_template.FormatSectionHeader(
                    "CHECKING MAIN FILES INTEGRITY..."));
                FormatMainFilesCheck(sections.MainFilesCheck, report);
            }

            // FCX Mode Specific: Game Files Check
            if (sections.GameFilesCheck != null)
            {
                report.AppendLine(_template.FormatSectionHeader(
                    "CHECKING GAME FILES..."));
                FormatGameFilesCheck(sections.GameFilesCheck, report);
            }

            // Crash Suspects
            if (sections.CrashSuspects?.Any() == true)
            {
                report.AppendLine(_template.FormatSectionHeader(
                    "CHECKING FOR CRASH CAUSE SUSPECTS..."));
                FormatSuspectsSection(sections.CrashSuspects, report);
            }

            // Plugin Suspects
            if (sections.PluginSuspects?.Any() == true)
            {
                report.AppendLine(_template.FormatSectionHeader(
                    "SCANNING THE LOG FOR SPECIFIC (POSSIBLE) SUSPECTS..."));
                FormatPluginSection(sections.PluginSuspects, report);
            }

            // FormID Analysis
            if (sections.FormIdSuspects?.Any() == true)
            {
                report.AppendLine(_template.FormatSectionHeader(
                    "CHECKING FOR POSSIBLE FORM ID SUSPECTS..."));
                FormatFormIdSection(sections.FormIdSuspects, report);
            }

            // Named Records
            if (sections.NamedRecords?.Any() == true)
            {
                report.AppendLine(_template.FormatSectionHeader(
                    "CHECKING FOR NAMED RECORDS..."));
                FormatNamedRecordsSection(sections.NamedRecords, report);
            }

            // Performance Metrics (FCX Mode)
            if (sections.Performance != null)
            {
                report.AppendLine(_template.FormatSectionHeader(
                    "PERFORMANCE ANALYSIS"));
                FormatPerformanceSection(sections.Performance, report);
            }

            // Game Hints (FCX Mode)
            if (sections.GameHints?.Hints.Any() == true)
            {
                report.AppendLine(_template.FormatSectionHeader(
                    "GAME TIPS & HINTS"));
                FormatGameHintsSection(sections.GameHints, report);
            }

            // Footer
            if (sections.Footer != null)
            {
                report.AppendLine(_template.FormatFooter(
                    sections.Footer.Version,
                    sections.Footer.GeneratedDate));
            }
        }

        private string FormatFCXNotice(FCXNoticeSection notice)
        {
            var sb = new StringBuilder();
            
            if (notice.IsEnabled)
            {
                sb.AppendLine("* NOTICE: FCX MODE IS ENABLED. CLASSIC MUST BE RUN BY THE ORIGINAL USER FOR CORRECT DETECTION *");
                sb.AppendLine("[ To disable mod & game files detection, disable FCX Mode in the exe or CLASSIC Settings.yaml ]");
            }
            else
            {
                sb.AppendLine("* NOTICE: FCX MODE IS DISABLED. YOU CAN ENABLE IT TO DETECT PROBLEMS IN YOUR MOD & GAME FILES *");
                sb.AppendLine("[ FCX Mode can be enabled in the exe or CLASSIC Settings.yaml located in your CLASSIC folder. ]");
            }
            
            sb.AppendLine();
            return sb.ToString();
        }

        private string FormatErrorSection(ErrorSection error)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Main Error: {error.MainError}");
            sb.AppendLine($"Detected {error.CrashGenName} Version: {error.CrashGenVersion}");
            
            if (error.IsOutdated)
            {
                sb.AppendLine($"* ⚠️ WARNING: Your {error.CrashGenName} is outdated! Please update to the latest version. *");
            }
            else
            {
                sb.AppendLine($"* You have the latest version of {error.CrashGenName}! *");
            }
            
            return sb.ToString();
        }

        private void FormatSettingsSection(SettingsValidationSection settings, StringBuilder report)
        {
            foreach (var issue in settings.Issues.OrderByDescending(i => i.SeverityScore))
            {
                report.AppendLine(_template.FormatError(
                    issue.SettingName,
                    $"{issue.Description} (Current: {issue.CurrentValue}, Expected: {issue.ExpectedValue})",
                    issue.SeverityScore));
            }
        }

        private void FormatMainFilesCheck(MainFilesCheckSection mainFiles, StringBuilder report)
        {
            foreach (var file in mainFiles.FileChecks)
            {
                var status = file.IsValid ? "✓" : "✗";
                var message = file.IsValid ? "Valid" : file.ErrorMessage;
                report.AppendLine($"{status} {file.FileName}: {message}");
            }
        }

        private void FormatGameFilesCheck(GameFilesCheckSection gameFiles, StringBuilder report)
        {
            if (gameFiles.MissingFiles.Any())
            {
                report.AppendLine("Missing Game Files:");
                foreach (var file in gameFiles.MissingFiles)
                {
                    report.AppendLine($"  - {file}");
                }
            }

            if (gameFiles.CorruptedFiles.Any())
            {
                report.AppendLine("\nCorrupted Game Files:");
                foreach (var file in gameFiles.CorruptedFiles)
                {
                    report.AppendLine($"  - {file.FileName} ({file.Issue})");
                }
            }
        }

        private void FormatSuspectsSection(List<SuspectSection> suspects, StringBuilder report)
        {
            // Group by severity and sort
            var groupedSuspects = suspects
                .GroupBy(s => s.Severity)
                .OrderByDescending(g => g.Key);

            foreach (var group in groupedSuspects)
            {
                foreach (var suspect in group.OrderBy(s => s.Name))
                {
                    report.AppendLine(_template.FormatSuspect(
                        suspect.Name,
                        suspect.Description,
                        suspect.Severity,
                        suspect.Evidence,
                        suspect.Recommendation));
                }
            }
        }

        private void FormatPluginSection(List<PluginSection> plugins, StringBuilder report)
        {
            // Check for plugin limit
            var ffPrefixCount = plugins.Count(p => p.LoadOrder >= 0xFF);
            if (ffPrefixCount > 0)
            {
                report.AppendLine($"⚠️ WARNING: {ffPrefixCount} plugins with FF prefix detected!");
                report.AppendLine("This may cause instability. Consider merging or removing plugins.\n");
            }

            report.AppendLine("# LIST OF (POSSIBLE) PLUGIN SUSPECTS #");
            
            foreach (var plugin in plugins.OrderBy(p => p.LoadOrder))
            {
                report.AppendLine(_template.FormatPlugin(
                    plugin.LoadOrder,
                    plugin.Name,
                    plugin.Status,
                    plugin.Flags));
            }
        }

        private void FormatFormIdSection(List<FormIdSection> formIds, StringBuilder report)
        {
            foreach (var formId in formIds)
            {
                report.AppendLine(_template.FormatFormId(
                    formId.FormId,
                    formId.PluginIndex,
                    formId.LocalFormId,
                    formId.PluginName,
                    formId.FormType));
            }
        }

        private void FormatNamedRecordsSection(List<NamedRecordSection> records, StringBuilder report)
        {
            foreach (var record in records.OrderBy(r => r.RecordName))
            {
                report.AppendLine($"- {record.RecordName} [{record.FormId:X8}]");
                if (!string.IsNullOrEmpty(record.PluginName))
                {
                    report.AppendLine($"  Plugin: {record.PluginName}");
                }
                if (record.Issues.Any())
                {
                    report.AppendLine($"  Issues: {string.Join(", ", record.Issues)}");
                }
            }
        }

        private void FormatPerformanceSection(PerformanceMetricsSection performance, StringBuilder report)
        {
            report.AppendLine($"Processing Time: {performance.ProcessingTime.TotalSeconds:F2}s");
            report.AppendLine($"Files Analyzed: {performance.FilesAnalyzed}");
            report.AppendLine($"Patterns Matched: {performance.PatternsMatched}");
            
            if (performance.MemoryUsageMB > 0)
            {
                report.AppendLine($"Memory Usage: {performance.MemoryUsageMB:F1} MB");
            }
        }

        private void FormatGameHintsSection(GameHintsSection hints, StringBuilder report)
        {
            foreach (var hint in hints.Hints)
            {
                report.AppendLine($"💡 {hint}");
            }
        }

        private string GenerateFallbackReport(CrashLogAnalysisResult analysisResult)
        {
            return $"# Crash Log Analysis Report\n\n" +
                   $"**File:** {analysisResult.CrashLog.FileName}\n" +
                   $"**Error:** Failed to generate comprehensive report\n\n" +
                   "An error occurred during report generation. Please check the logs for details.\n";
        }
    }

    /// <summary>
    /// Factory for creating report strategies
    /// </summary>
    public interface IReportStrategyFactory
    {
        IReportStrategy GetStrategy(bool fcxMode);
    }

    /// <summary>
    /// Report generation strategy interface
    /// </summary>
    public interface IReportStrategy
    {
        string Name { get; }
        bool RequiresFCXMode { get; }
        
        Task<ReportSections> GenerateSectionsAsync(
            CrashLogAnalysisResult analysisResult,
            IEnumerable<ISectionGenerator> generators,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Standard report strategy (non-FCX mode)
    /// </summary>
    public class StandardReportStrategy : IReportStrategy
    {
        public string Name => "Standard Report";
        public bool RequiresFCXMode => false;

        public async Task<ReportSections> GenerateSectionsAsync(
            CrashLogAnalysisResult analysisResult,
            IEnumerable<ISectionGenerator> generators,
            CancellationToken cancellationToken)
        {
            var sections = new ReportSections();
            
            // Generate only standard sections
            var standardGenerators = generators.Where(g => 
                g is IHeaderSectionGenerator ||
                g is IErrorSectionGenerator ||
                g is ISettingsSectionGenerator ||
                g is ISuspectSectionGenerator ||
                g is IPluginSectionGenerator ||
                g is IFormIdSectionGenerator ||
                g is INamedRecordSectionGenerator ||
                g is IFooterSectionGenerator);

            // Run generators in parallel where possible
            var tasks = standardGenerators.Select(async g =>
            {
                switch (g)
                {
                    case IHeaderSectionGenerator header:
                        sections.Header = await header.GenerateAsync(analysisResult, cancellationToken);
                        break;
                    case IErrorSectionGenerator error:
                        sections.MainError = await error.GenerateAsync(analysisResult, cancellationToken);
                        break;
                    // ... other generators
                }
            });

            await Task.WhenAll(tasks);
            return sections;
        }
    }

    /// <summary>
    /// Advanced report strategy (FCX mode)
    /// </summary>
    public class AdvancedReportStrategy : IReportStrategy
    {
        public string Name => "Advanced Report (FCX Mode)";
        public bool RequiresFCXMode => true;

        public async Task<ReportSections> GenerateSectionsAsync(
            CrashLogAnalysisResult analysisResult,
            IEnumerable<ISectionGenerator> generators,
            CancellationToken cancellationToken)
        {
            var sections = new ReportSections();
            
            // Generate all sections including FCX-specific ones
            var tasks = generators.Select(async g =>
            {
                switch (g)
                {
                    case IHeaderSectionGenerator header:
                        sections.Header = await header.GenerateAsync(analysisResult, cancellationToken);
                        break;
                    case IFCXNoticeSectionGenerator fcxNotice:
                        sections.FCXNotice = await fcxNotice.GenerateAsync(analysisResult, cancellationToken);
                        break;
                    case IMainFilesCheckGenerator mainFiles:
                        sections.MainFilesCheck = await mainFiles.GenerateAsync(analysisResult, cancellationToken);
                        break;
                    case IGameFilesCheckGenerator gameFiles:
                        sections.GameFilesCheck = await gameFiles.GenerateAsync(analysisResult, cancellationToken);
                        break;
                    case IPerformanceMetricsGenerator performance:
                        sections.Performance = await performance.GenerateAsync(analysisResult, cancellationToken);
                        break;
                    case IGameHintsGenerator hints:
                        sections.GameHints = await hints.GenerateAsync(analysisResult, cancellationToken);
                        break;
                    // ... all other generators
                }
            });

            await Task.WhenAll(tasks);
            return sections;
        }
    }
}