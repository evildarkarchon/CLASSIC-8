using Classic.Core.Enums;
using Classic.Core.Models;

namespace Classic.ScanLog.Models;

/// <summary>
/// Comprehensive data structure for advanced report generation
/// </summary>
public class AdvancedReportData
{
    public string FileName { get; set; } = string.Empty;
    public string ClassicVersion { get; set; } = "1.0.0";
    public DateTime GeneratedDate { get; set; } = DateTime.Now;
    public TimeSpan ProcessingTime { get; set; }

    // Core crash information
    public CrashLog CrashLog { get; set; } = new();
    public List<Suspect> CrashSuspects { get; set; } = new();
    public List<ModConflictResult> ModConflicts { get; set; } = new();
    public List<FileValidationResult> FileValidationResults { get; set; } = new();

    // Analysis results
    public List<PluginInfo> ProblematicPlugins { get; set; } = new();
    public List<FormIdSuspect> SuspectFormIds { get; set; } = new();
    public List<NamedRecord> NamedRecords { get; set; } = new();

    // System information
    public SystemInfo SystemInfo { get; set; } = new();
    public GameInfo GameInfo { get; set; } = new();
    public SettingsValidation SettingsValidation { get; set; } = new();

    // Performance metrics
    public PerformanceMetrics Performance { get; set; } = new();
    public List<string> GameHints { get; set; } = new();

    // Categorized issues
    public List<ReportIssue> CriticalIssues { get; set; } = new();
    public List<ReportIssue> Errors { get; set; } = new();
    public List<ReportIssue> Warnings { get; set; } = new();
    public List<ReportIssue> Recommendations { get; set; } = new();
}

/// <summary>
/// System information for reporting
/// </summary>
public class SystemInfo
{
    public string OperatingSystem { get; set; } = string.Empty;
    public string Cpu { get; set; } = string.Empty;
    public string Gpu { get; set; } = string.Empty;
    public string Memory { get; set; } = string.Empty;
    public GpuManufacturer GpuManufacturer { get; set; }
}

/// <summary>
/// Game information for reporting
/// </summary>
public class GameInfo
{
    public GameId GameId { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public string CrashGenName { get; set; } = string.Empty;
    public string CrashGenVersion { get; set; } = string.Empty;
    public bool IsOutdated { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
}

/// <summary>
/// Settings validation results
/// </summary>
public class SettingsValidation
{
    public List<SettingsIssue> Issues { get; set; } = new();
    public bool HasCriticalIssues => Issues.Any(i => i.Severity >= 4);
}

/// <summary>
/// Individual settings validation issue
/// </summary>
public class SettingsIssue
{
    public string SettingName { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Severity { get; set; } // 1-5 scale
}

/// <summary>
/// FormID suspect information
/// </summary>
public class FormIdSuspect
{
    public uint FormIdValue { get; set; }
    public byte PluginIndex { get; set; }
    public uint LocalFormId { get; set; }
    public string PluginName { get; set; } = string.Empty;
    public string FormType { get; set; } = string.Empty;
    public string? ResolvedName { get; set; }
}

/// <summary>
/// Named record information
/// </summary>
public class NamedRecord
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public FormIdSuspect FormId { get; set; } = new();
    public PluginInfo OriginPlugin { get; set; } = new();
}

/// <summary>
/// Categorized report issue
/// </summary>
public class ReportIssue
{
    public ReportIssueType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public string? DocumentationUrl { get; set; }
    public int Severity { get; set; } // 1-5 scale
    public List<string> AffectedFiles { get; set; } = new();
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Types of report issues
/// </summary>
public enum ReportIssueType
{
    CrashSuspect,
    ModConflict,
    PluginIssue,
    FileValidation,
    SettingsIssue,
    PerformanceIssue,
    SystemCompatibility,
    GameConfiguration
}
