using System;
using System.Collections.Generic;
using System.Linq;

namespace Classic.ScanLog.Models
{
    /// <summary>
    /// Base class for report data models
    /// </summary>
    public abstract class ReportDataModel
    {
        // Common properties
        public string FileName { get; set; }
        public string ClassicVersion { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string GeneratedDateTime => GeneratedDate.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Error Information
        public string MainError { get; set; }
        public string CrashGenName { get; set; }
        public string CrashGenVersion { get; set; }
        public string GameVersion { get; set; }
        public bool IsOutdated { get; set; }
        public string LatestCrashGenVersion { get; set; }
        
        // Analysis Results
        public List<SettingsIssueModel> SettingsIssues { get; set; } = new();
        public List<CrashSuspectModel> CrashSuspects { get; set; } = new();
        public List<ModConflictModel> ModConflicts { get; set; } = new();
        public List<PluginModel> ProblematicPlugins { get; set; } = new();
        public List<FormIdSuspectModel> FormIdSuspects { get; set; } = new();
        public List<NamedRecordModel> NamedRecords { get; set; } = new();
        
        // Summary Information
        public bool HasCriticalIssues => CrashSuspects.Any(s => s.Severity >= 5) || 
                                         SettingsIssues.Any(s => s.Severity >= 5);
        public bool HasWarnings => CrashSuspects.Any(s => s.Severity >= 3) || 
                                  SettingsIssues.Any(s => s.Severity >= 3);
        
        // Plugin Limit Information
        public bool PluginLimitWarning { get; set; }
        public int FFPrefixCount { get; set; }
    }

    /// <summary>
    /// Standard report data model (non-FCX mode)
    /// </summary>
    public class StandardReportModel : ReportDataModel
    {
        public List<ModConflictGroupModel> ModConflictGroups => ModConflicts
            .GroupBy(m => m.ConflictType)
            .Select(g => new ModConflictGroupModel
            {
                GroupName = GetConflictGroupName(g.Key),
                GroupIcon = GetConflictGroupIcon(g.Key),
                Conflicts = g.ToList()
            })
            .ToList();

        private string GetConflictGroupName(string conflictType)
        {
            return conflictType switch
            {
                "incompatible" => "Incompatible Mods",
                "conflict" => "Conflicting Mods",
                "missing" => "Missing Dependencies",
                "outdated" => "Outdated Mods",
                _ => "Other Issues"
            };
        }

        private string GetConflictGroupIcon(string conflictType)
        {
            return conflictType switch
            {
                "incompatible" => "❌",
                "conflict" => "⚔️",
                "missing" => "❓",
                "outdated" => "🕐",
                _ => "⚠️"
            };
        }
    }

    /// <summary>
    /// Enhanced report data model (non-FCX mode with advanced formatting)
    /// </summary>
    public class EnhancedReportModel : StandardReportModel
    {
        // Additional properties for enhanced formatting
        public string GameName { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public double ProcessingTimeSeconds => ProcessingTime.TotalSeconds;
        
        // Additional error details
        public string CrashAddress { get; set; }
        public string ProbableCause { get; set; }
        
        // Enhanced crash analysis with priority grouping
        public List<CrashSuspectPriorityGroup> CrashSuspectsByPriority => CrashSuspects
            .GroupBy(s => GetPriorityFromSeverity(s.Severity))
            .Select(g => new CrashSuspectPriorityGroup
            {
                Priority = g.Key,
                Suspects = g.ToList()
            })
            .OrderBy(g => g.Priority)
            .ToList();
        
        // Mod compatibility with categories
        public List<ModConflictCategoryModel> ModConflictCategories { get; set; } = new();
        public int CompatibilityScore { get; set; }
        
        // Plugin analysis
        public PluginLimitStatusModel PluginLimitStatus { get; set; }
        
        // FormID analysis
        public string FormIdAnalysisSummary { get; set; }
        
        // Named records by type
        public List<NamedRecordTypeGroup> NamedRecordsByType => NamedRecords
            .GroupBy(r => r.RecordType)
            .Select(g => new NamedRecordTypeGroup
            {
                RecordType = g.Key,
                Records = g.ToList()
            })
            .ToList();
        
        // Performance metrics (without FCX file checking)
        public string PerformanceIcon { get; set; }
        public string PerformanceAssessment { get; set; }
        public int TotalFilesAnalyzed { get; set; }
        public int PatternMatchCount { get; set; }
        public int SuspectsFound => CrashSuspects.Count;
        public int PluginsAnalyzed => ProblematicPlugins.Count;
        public int FormIdsChecked => FormIdSuspects.Count;
        public List<string> PerformanceRecommendations { get; set; } = new();
        
        // Game hints (same as Python - not FCX-specific)
        public List<string> GameHints { get; set; } = new();
        
        // Executive summary
        public int CriticalCount => CrashSuspects.Count(s => s.Severity >= 5) + 
                                   SettingsIssues.Count(s => s.Severity >= 5);
        public int HighCount => CrashSuspects.Count(s => s.Severity == 4) + 
                               SettingsIssues.Count(s => s.Severity == 4);
        public int MediumCount => CrashSuspects.Count(s => s.Severity == 3) + 
                                 SettingsIssues.Count(s => s.Severity == 3);
        public int LowCount => CrashSuspects.Count(s => s.Severity <= 2) + 
                              SettingsIssues.Count(s => s.Severity <= 2);
        
        public List<TopActionModel> TopActions { get; set; } = new();
        public int StabilityScore { get; set; }
        public string RiskLevel { get; set; }
        public string OverallRecommendation { get; set; }
        public bool EnableFCXSuggestion { get; set; } = true;

        private int GetPriorityFromSeverity(int severity)
        {
            return severity switch
            {
                6 => 1, // Critical
                5 => 1, // Severe
                4 => 2, // High
                3 => 3, // Medium
                _ => 4  // Low
            };
        }
    }

    /// <summary>
    /// Advanced report data model (FCX mode)
    /// </summary>
    public class AdvancedReportModel : ReportDataModel
    {
        // FCX-specific properties
        public string GameName { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public double ProcessingTimeSeconds => ProcessingTime.TotalSeconds;
        
        // Additional error details
        public string CrashAddress { get; set; }
        public string ProbableCause { get; set; }
        
        // Main files validation
        public List<MainFileCheckModel> MainFilesChecks { get; set; } = new();
        public int ValidFilesCount => MainFilesChecks.Count(f => f.IsValid);
        public int TotalFilesCount => MainFilesChecks.Count;
        
        // Game files validation
        public List<MissingGameFileModel> MissingGameFiles { get; set; } = new();
        public List<CorruptedGameFileModel> CorruptedGameFiles { get; set; } = new();
        
        // Enhanced crash analysis
        public List<CrashSuspectPriorityGroup> CrashSuspectsByPriority => CrashSuspects
            .GroupBy(s => GetPriorityFromSeverity(s.Severity))
            .Select(g => new CrashSuspectPriorityGroup
            {
                Priority = g.Key,
                Suspects = g.ToList()
            })
            .OrderBy(g => g.Priority)
            .ToList();
        
        // Mod compatibility
        public List<ModConflictCategoryModel> ModConflictCategories { get; set; } = new();
        public int CompatibilityScore { get; set; }
        
        // Plugin analysis
        public PluginLimitStatusModel PluginLimitStatus { get; set; }
        
        // FormID analysis
        public string FormIdAnalysisSummary { get; set; }
        
        // Named records by type
        public List<NamedRecordTypeGroup> NamedRecordsByType => NamedRecords
            .GroupBy(r => r.RecordType)
            .Select(g => new NamedRecordTypeGroup
            {
                RecordType = g.Key,
                Records = g.ToList()
            })
            .ToList();
        
        // Performance metrics
        public string PerformanceIcon { get; set; }
        public string PerformanceAssessment { get; set; }
        public int WorkerThreadsUsed { get; set; }
        public int TotalFilesProcessed { get; set; }
        public double PatternMatchTime { get; set; }
        public double FileIOTime { get; set; }
        public double AnalysisTime { get; set; }
        public double ReportGenTime { get; set; }
        public double PatternMatchPercentage => (PatternMatchTime / ProcessingTimeSeconds) * 100;
        public double FileIOPercentage => (FileIOTime / ProcessingTimeSeconds) * 100;
        public double AnalysisPercentage => (AnalysisTime / ProcessingTimeSeconds) * 100;
        public double ReportGenPercentage => (ReportGenTime / ProcessingTimeSeconds) * 100;
        public List<string> PerformanceRecommendations { get; set; } = new();
        
        // Game hints
        public List<string> GameHints { get; set; } = new();
        
        // Executive summary
        public int CriticalCount => CrashSuspects.Count(s => s.Severity >= 5) + 
                                   SettingsIssues.Count(s => s.Severity >= 5);
        public int HighCount => CrashSuspects.Count(s => s.Severity == 4) + 
                               SettingsIssues.Count(s => s.Severity == 4);
        public int MediumCount => CrashSuspects.Count(s => s.Severity == 3) + 
                                 SettingsIssues.Count(s => s.Severity == 3);
        public int LowCount => CrashSuspects.Count(s => s.Severity <= 2) + 
                              SettingsIssues.Count(s => s.Severity <= 2);
        
        public List<TopActionModel> TopActions { get; set; } = new();
        public int StabilityScore { get; set; }
        public string RiskLevel { get; set; }
        public string OverallRecommendation { get; set; }

        private int GetPriorityFromSeverity(int severity)
        {
            return severity switch
            {
                6 => 1, // Critical
                5 => 1, // Severe
                4 => 2, // High
                3 => 3, // Medium
                _ => 4  // Low
            };
        }
    }

    // Supporting model classes

    public class SettingsIssueModel
    {
        public string SettingName { get; set; }
        public string Status { get; set; }
        public string CurrentValue { get; set; }
        public string ExpectedValue { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
        public int Severity { get; set; }
        public string SeverityIcon => GetSeverityIcon(Severity);

        private string GetSeverityIcon(int severity)
        {
            return severity switch
            {
                6 => "💀",
                5 => "🔴",
                4 => "⚠️",
                3 => "⚡",
                2 => "🔵",
                1 => "ℹ️",
                _ => "❓"
            };
        }
    }

    public class CrashSuspectModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Evidence { get; set; }
        public string Recommendation { get; set; }
        public List<string> RelatedFiles { get; set; } = new();
        public int Severity { get; set; }
        public string SeverityIcon => GetSeverityIcon(Severity);
        public string SeverityBadge => GetSeverityBadge(Severity);
        
        // Advanced properties
        public int ConfidencePercentage { get; set; }
        public string Category { get; set; }
        public List<string> EvidenceItems { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public string TechnicalDetails { get; set; }

        private string GetSeverityIcon(int severity)
        {
            return severity switch
            {
                6 => "💀",
                5 => "🔴",
                4 => "⚠️",
                3 => "⚡",
                2 => "🔵",
                1 => "ℹ️",
                _ => "❓"
            };
        }

        private string GetSeverityBadge(int severity)
        {
            return severity switch
            {
                6 => "![CRITICAL](https://img.shields.io/badge/CRITICAL-red?style=flat-square)",
                5 => "![SEVERE](https://img.shields.io/badge/SEVERE-orange?style=flat-square)",
                4 => "![HIGH](https://img.shields.io/badge/HIGH-yellow?style=flat-square)",
                3 => "![MEDIUM](https://img.shields.io/badge/MEDIUM-blue?style=flat-square)",
                2 => "![LOW](https://img.shields.io/badge/LOW-green?style=flat-square)",
                1 => "![INFO](https://img.shields.io/badge/INFO-lightgrey?style=flat-square)",
                _ => "![UNKNOWN](https://img.shields.io/badge/UNKNOWN-grey?style=flat-square)"
            };
        }
    }

    public class ModConflictModel
    {
        public string ModName { get; set; }
        public string ModVersion { get; set; }
        public string Warning { get; set; }
        public string Solution { get; set; }
        public List<string> ConflictsWith { get; set; } = new();
        public string ConflictType { get; set; }
        public string KnownIssues { get; set; }
        public List<string> AlternativeMods { get; set; } = new();
    }

    public class PluginModel
    {
        public int LoadOrder { get; set; }
        public string LoadOrderHex => $"{LoadOrder:X2}";
        public string Name { get; set; }
        public string Status { get; set; }
        public string StatusBadge { get; set; }
        public string Flags { get; set; }
        public string Issues { get; set; }
        public List<string> Dependencies { get; set; } = new();
        public List<string> Patches { get; set; } = new();
    }

    public class FormIdSuspectModel
    {
        public uint FormId { get; set; }
        public string FormIdHex => $"{FormId:X8}";
        public byte PluginIndex { get; set; }
        public string PluginIndexHex => $"{PluginIndex:X2}";
        public uint LocalFormId { get; set; }
        public string LocalFormIdHex => $"{LocalFormId:X6}";
        public string PluginName { get; set; }
        public string FormType { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string StatusIcon { get; set; }
        public string CorruptionType { get; set; }
        public List<string> RelatedRecords { get; set; } = new();
    }

    public class NamedRecordModel
    {
        public string RecordName { get; set; }
        public uint FormId { get; set; }
        public string FormIdHex => $"{FormId:X8}";
        public string PluginName { get; set; }
        public List<string> Issues { get; set; } = new();
        public string RecordType { get; set; }
        public List<string> Overrides { get; set; } = new();
    }

    // FCX-specific models

    public class MainFileCheckModel
    {
        public string FileName { get; set; }
        public bool IsValid { get; set; }
        public string StatusIcon => IsValid ? "✅" : "❌";
        public string FileSize { get; set; }
        public string ErrorMessage { get; set; }
        public string RecommendedAction { get; set; }
    }

    public class MissingGameFileModel
    {
        public string FileName { get; set; }
        public string Impact { get; set; }
        public string Solution { get; set; }
    }

    public class CorruptedGameFileModel
    {
        public string FileName { get; set; }
        public string Issue { get; set; }
        public string ExpectedSize { get; set; }
        public string ActualSize { get; set; }
        public string RecommendedAction { get; set; }
    }

    public class PluginLimitStatusModel
    {
        public int TotalPlugins { get; set; }
        public int LightPlugins { get; set; }
        public int RegularPlugins { get; set; }
        public int FFPrefixCount { get; set; }
        public bool ExceedsLimit => RegularPlugins > 254;
    }

    public class TopActionModel
    {
        public string Action { get; set; }
        public string Impact { get; set; }
    }

    // Grouping models

    public class ModConflictGroupModel
    {
        public string GroupName { get; set; }
        public string GroupIcon { get; set; }
        public List<ModConflictModel> Conflicts { get; set; }
    }

    public class ModConflictCategoryModel
    {
        public string CategoryName { get; set; }
        public string CategoryIcon { get; set; }
        public List<ModConflictModel> Conflicts { get; set; }
    }

    public class CrashSuspectPriorityGroup
    {
        public int Priority { get; set; }
        public List<CrashSuspectModel> Suspects { get; set; }
    }

    public class NamedRecordTypeGroup
    {
        public string RecordType { get; set; }
        public List<NamedRecordModel> Records { get; set; }
    }
}