using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Classic.Core.Models;
using Classic.ScanLog.Models;
using Classic.ScanLog.Services;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Examples
{
    /// <summary>
    /// Comprehensive examples showing when and how to use each report template
    /// </summary>
    public class ReportTemplateSelectionGuide
    {
        private readonly ReportTemplateService _templateService;
        private readonly ILogger<ReportTemplateSelectionGuide> _logger;

        public ReportTemplateSelectionGuide(
            ReportTemplateService templateService,
            ILogger<ReportTemplateSelectionGuide> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        /// <summary>
        /// Demonstrates the decision logic for selecting the appropriate template
        /// </summary>
        public async Task<string> GenerateReportAsync(
            CrashLogAnalysisResult analysisResult,
            UserPreferences preferences)
        {
            _logger.LogInformation("Selecting report template based on user preferences");

            // Decision tree for template selection
            if (preferences.FCXMode)
            {
                _logger.LogInformation("FCX Mode enabled - using Advanced template");
                return await GenerateAdvancedReport(analysisResult);
            }
            else if (preferences.UseEnhancedFormatting)
            {
                _logger.LogInformation("Enhanced formatting requested - using Enhanced template");
                return await GenerateEnhancedReport(analysisResult);
            }
            else
            {
                _logger.LogInformation("Using Standard template");
                return await GenerateStandardReport(analysisResult);
            }
        }

        /// <summary>
        /// Example 1: Standard Report
        /// Use when: Basic crash analysis is sufficient, minimal formatting needed
        /// </summary>
        private async Task<string> GenerateStandardReport(CrashLogAnalysisResult analysisResult)
        {
            var model = new StandardReportModel
            {
                // Basic metadata
                FileName = analysisResult.CrashLog.FileName,
                ClassicVersion = analysisResult.ClassicVersion,
                GeneratedDate = DateTime.Now,
                
                // Error information
                MainError = analysisResult.CrashLog.MainError,
                CrashGenName = analysisResult.CrashGenName,
                CrashGenVersion = analysisResult.CrashLog.CrashGenVersion,
                GameVersion = analysisResult.CrashLog.GameVersion,
                IsOutdated = analysisResult.IsOutdated,
                LatestCrashGenVersion = analysisResult.LatestCrashGenVersion,
                
                // Analysis results
                SettingsIssues = MapSettingsIssues(analysisResult.SettingsValidation),
                CrashSuspects = MapCrashSuspects(analysisResult.CrashSuspects),
                ModConflicts = MapModConflicts(analysisResult.ModCompatibilityIssues),
                ProblematicPlugins = MapPlugins(analysisResult.ProblematicPlugins),
                FormIdSuspects = MapFormIds(analysisResult.FormIdIssues),
                NamedRecords = MapNamedRecords(analysisResult.NamedRecords),
                
                // Plugin limit info
                PluginLimitWarning = analysisResult.PluginLimitStatus.ExceedsLimit,
                FFPrefixCount = analysisResult.PluginLimitStatus.FFPrefixCount
            };

            return await _templateService.GenerateStandardReportAsync(model);
        }

        /// <summary>
        /// Example 2: Enhanced Report (Non-FCX with Advanced Formatting)
        /// Use when: User wants better formatting and analysis without FCX mode
        /// </summary>
        private async Task<string> GenerateEnhancedReport(CrashLogAnalysisResult analysisResult)
        {
            var model = new EnhancedReportModel
            {
                // All standard properties
                FileName = analysisResult.CrashLog.FileName,
                ClassicVersion = analysisResult.ClassicVersion,
                GeneratedDate = DateTime.Now,
                MainError = analysisResult.CrashLog.MainError,
                CrashGenName = analysisResult.CrashGenName,
                CrashGenVersion = analysisResult.CrashLog.CrashGenVersion,
                GameVersion = analysisResult.CrashLog.GameVersion,
                IsOutdated = analysisResult.IsOutdated,
                LatestCrashGenVersion = analysisResult.LatestCrashGenVersion,
                
                // Enhanced properties
                GameName = GetGameDisplayName(analysisResult.GameInfo),
                ProcessingTime = analysisResult.ProcessingTime,
                CrashAddress = analysisResult.CrashLog.CrashAddress,
                ProbableCause = analysisResult.CrashLog.ProbableCause,
                
                // Enhanced analysis with better grouping
                SettingsIssues = MapSettingsIssuesEnhanced(analysisResult.SettingsValidation),
                CrashSuspects = MapCrashSuspectsEnhanced(analysisResult.CrashSuspects),
                ModConflictCategories = MapModConflictCategories(analysisResult.ModCompatibilityIssues),
                ProblematicPlugins = MapPluginsEnhanced(analysisResult.ProblematicPlugins),
                FormIdSuspects = MapFormIdsEnhanced(analysisResult.FormIdIssues),
                NamedRecords = MapNamedRecordsEnhanced(analysisResult.NamedRecords),
                
                // Compatibility and plugin analysis
                CompatibilityScore = CalculateCompatibilityScore(analysisResult),
                PluginLimitStatus = MapPluginLimitStatus(analysisResult.PluginLimitStatus),
                FormIdAnalysisSummary = GenerateFormIdSummary(analysisResult.FormIdIssues),
                
                // Performance metrics (non-FCX)
                PerformanceIcon = GetPerformanceIcon(analysisResult.ProcessingTime),
                PerformanceAssessment = GetPerformanceAssessment(analysisResult.ProcessingTime),
                TotalFilesAnalyzed = analysisResult.Statistics.FilesAnalyzed,
                PatternMatchCount = analysisResult.Statistics.PatternsMatched,
                PerformanceRecommendations = GetPerformanceRecommendations(analysisResult),
                
                // Game hints (same as Python - not FCX-specific)
                GameHints = await LoadGameHints(analysisResult.GameInfo),
                
                // Executive summary
                TopActions = GenerateTopActions(analysisResult),
                StabilityScore = CalculateStabilityScore(analysisResult),
                RiskLevel = DetermineRiskLevel(analysisResult),
                OverallRecommendation = GenerateOverallRecommendation(analysisResult),
                EnableFCXSuggestion = true // Suggest FCX mode for deeper analysis
            };

            return await _templateService.GenerateEnhancedReportAsync(model);
        }

        /// <summary>
        /// Example 3: Advanced Report (FCX Mode Enabled)
        /// Use when: FCX mode is enabled, full file system analysis available
        /// </summary>
        private async Task<string> GenerateAdvancedReport(CrashLogAnalysisResult analysisResult)
        {
            var model = new AdvancedReportModel
            {
                // All enhanced properties
                FileName = analysisResult.CrashLog.FileName,
                ClassicVersion = analysisResult.ClassicVersion,
                GeneratedDate = DateTime.Now,
                GameName = GetGameDisplayName(analysisResult.GameInfo),
                ProcessingTime = analysisResult.ProcessingTime,
                
                // Include all enhanced analysis
                MainError = analysisResult.CrashLog.MainError,
                CrashGenName = analysisResult.CrashGenName,
                CrashGenVersion = analysisResult.CrashLog.CrashGenVersion,
                GameVersion = analysisResult.CrashLog.GameVersion,
                IsOutdated = analysisResult.IsOutdated,
                CrashAddress = analysisResult.CrashLog.CrashAddress,
                ProbableCause = analysisResult.CrashLog.ProbableCause,
                
                // All enhanced sections
                SettingsIssues = MapSettingsIssuesEnhanced(analysisResult.SettingsValidation),
                CrashSuspects = MapCrashSuspectsEnhanced(analysisResult.CrashSuspects),
                ModConflictCategories = MapModConflictCategories(analysisResult.ModCompatibilityIssues),
                ProblematicPlugins = MapPluginsEnhanced(analysisResult.ProblematicPlugins),
                FormIdSuspects = MapFormIdsEnhanced(analysisResult.FormIdIssues),
                NamedRecords = MapNamedRecordsEnhanced(analysisResult.NamedRecords),
                
                // FCX-specific sections
                MainFilesChecks = MapMainFilesChecks(analysisResult.MainFilesValidation),
                MissingGameFiles = MapMissingGameFiles(analysisResult.GameFilesValidation),
                CorruptedGameFiles = MapCorruptedGameFiles(analysisResult.GameFilesValidation),
                
                // Full performance metrics with FCX data
                PerformanceIcon = GetPerformanceIcon(analysisResult.ProcessingTime),
                PerformanceAssessment = GetPerformanceAssessment(analysisResult.ProcessingTime),
                WorkerThreadsUsed = analysisResult.Performance.WorkerThreadsUsed,
                TotalFilesProcessed = analysisResult.Performance.TotalFilesProcessed,
                PatternMatchTime = analysisResult.Performance.PatternMatchTime,
                FileIOTime = analysisResult.Performance.FileIOTime,
                AnalysisTime = analysisResult.Performance.AnalysisTime,
                ReportGenTime = analysisResult.Performance.ReportGenTime,
                PerformanceRecommendations = GetAdvancedPerformanceRecommendations(analysisResult),
                
                // Game-specific hints (FCX mode feature)
                GameHints = await LoadGameHints(analysisResult.GameInfo),
                
                // Complete executive summary
                TopActions = GenerateTopActionsWithFCX(analysisResult),
                StabilityScore = CalculateStabilityScoreWithFCX(analysisResult),
                RiskLevel = DetermineRiskLevelWithFCX(analysisResult),
                OverallRecommendation = GenerateAdvancedRecommendation(analysisResult)
            };

            return await _templateService.GenerateAdvancedReportAsync(model);
        }

        // Helper methods for mapping and calculations

        private List<SettingsIssueModel> MapSettingsIssues(SettingsValidation validation)
        {
            return validation.Issues.Select(issue => new SettingsIssueModel
            {
                SettingName = issue.SettingName,
                CurrentValue = issue.CurrentValue,
                ExpectedValue = issue.ExpectedValue,
                Description = issue.Description,
                Recommendation = issue.Recommendation,
                Severity = issue.SeverityScore
            }).ToList();
        }

        private List<SettingsIssueModel> MapSettingsIssuesEnhanced(SettingsValidation validation)
        {
            return validation.Issues.Select(issue => new SettingsIssueModel
            {
                SettingName = issue.SettingName,
                Status = GetSettingStatus(issue),
                CurrentValue = issue.CurrentValue,
                ExpectedValue = issue.ExpectedValue,
                Description = issue.Description,
                Recommendation = issue.Recommendation,
                Severity = issue.SeverityScore
            }).ToList();
        }

        private List<CrashSuspectModel> MapCrashSuspects(List<Suspect> suspects)
        {
            return suspects.Select(suspect => new CrashSuspectModel
            {
                Name = suspect.Name,
                Description = suspect.Description,
                Evidence = suspect.Evidence,
                Recommendation = suspect.Recommendation,
                RelatedFiles = suspect.RelatedFiles,
                Severity = suspect.SeverityScore
            }).ToList();
        }

        private List<CrashSuspectModel> MapCrashSuspectsEnhanced(List<Suspect> suspects)
        {
            return suspects.Select(suspect => new CrashSuspectModel
            {
                Name = suspect.Name,
                Description = suspect.Description,
                Evidence = suspect.Evidence,
                Recommendation = suspect.Recommendation,
                RelatedFiles = suspect.RelatedFiles,
                Severity = suspect.SeverityScore,
                // Enhanced properties
                ConfidencePercentage = CalculateConfidence(suspect),
                Category = DetermineCategory(suspect),
                EvidenceItems = ParseEvidenceItems(suspect.Evidence),
                Recommendations = ParseRecommendations(suspect.Recommendation),
                TechnicalDetails = suspect.TechnicalDetails
            }).ToList();
        }

        private List<string> GenerateAnalysisTips(CrashLogAnalysisResult analysisResult)
        {
            var tips = new List<string>();

            // Generate tips based on analysis results
            if (analysisResult.CrashSuspects.Any(s => s.Name.Contains("Memory")))
            {
                tips.Add("Consider increasing your page file size or adding more RAM");
            }

            if (analysisResult.ProblematicPlugins.Count > 100)
            {
                tips.Add("Your plugin count is very high. Consider merging plugins or using ESL flagged files");
            }

            if (analysisResult.ModCompatibilityIssues.Any())
            {
                tips.Add("Review your mod conflicts and ensure you have all required patches installed");
            }

            return tips;
        }

        private int CalculateCompatibilityScore(CrashLogAnalysisResult analysisResult)
        {
            var score = 100;

            // Deduct points for issues
            score -= analysisResult.ModCompatibilityIssues.Count * 5;
            score -= analysisResult.CrashSuspects.Count(s => s.SeverityScore >= 4) * 10;
            score -= analysisResult.SettingsValidation.Issues.Count(i => i.SeverityScore >= 4) * 5;

            return Math.Max(0, score);
        }

        private string GetPerformanceIcon(TimeSpan processingTime)
        {
            return processingTime.TotalSeconds switch
            {
                < 1 => "🚀",
                < 3 => "✅",
                < 5 => "⚡",
                _ => "🐌"
            };
        }

        private string GetPerformanceAssessment(TimeSpan processingTime)
        {
            return processingTime.TotalSeconds switch
            {
                < 1 => "Excellent",
                < 3 => "Good",
                < 5 => "Average",
                _ => "Slow - Consider optimizing your setup"
            };
        }

        // Additional helper methods would be implemented here...
    }

    /// <summary>
    /// User preferences for report generation
    /// </summary>
    public class UserPreferences
    {
        public bool FCXMode { get; set; }
        public bool UseEnhancedFormatting { get; set; }
        public string PreferredFormat { get; set; } = "Markdown";
        public bool IncludeDebugInfo { get; set; }
        public int MaxGameHints { get; set; } = 3;
    }

    /// <summary>
    /// Comparison of the three template types
    /// </summary>
    public static class TemplateComparisonGuide
    {
        public static void PrintComparison()
        {
            Console.WriteLine("CLASSIC-8 Report Template Comparison:");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            
            Console.WriteLine("1. STANDARD TEMPLATE");
            Console.WriteLine("   - Basic Markdown formatting");
            Console.WriteLine("   - Essential crash information");
            Console.WriteLine("   - Simple, clean layout");
            Console.WriteLine("   - Best for: Quick analysis, basic users");
            Console.WriteLine("   - File size: ~3-5 KB");
            Console.WriteLine();
            
            Console.WriteLine("2. ENHANCED TEMPLATE (Non-FCX)");
            Console.WriteLine("   - Advanced formatting with Unicode boxes");
            Console.WriteLine("   - Priority-grouped crash suspects");
            Console.WriteLine("   - Performance metrics (without file checks)");
            Console.WriteLine("   - Executive summary with action items");
            Console.WriteLine("   - Game-specific hints and tips");
            Console.WriteLine("   - Best for: Detailed analysis without FCX");
            Console.WriteLine("   - File size: ~8-12 KB");
            Console.WriteLine();
            
            Console.WriteLine("3. ADVANCED TEMPLATE (FCX Mode)");
            Console.WriteLine("   - Everything from Enhanced template");
            Console.WriteLine("   - Main files integrity checking");
            Console.WriteLine("   - Game files validation");
            Console.WriteLine("   - Full performance breakdown");
            Console.WriteLine("   - Extended file analysis metrics");
            Console.WriteLine("   - Best for: Complete system analysis");
            Console.WriteLine("   - File size: ~15-25 KB");
            Console.WriteLine();
            
            Console.WriteLine("Selection Logic:");
            Console.WriteLine("- FCX Mode Enabled → Advanced Template");
            Console.WriteLine("- Enhanced Formatting Requested → Enhanced Template");
            Console.WriteLine("- Default → Standard Template");
        }
    }
}