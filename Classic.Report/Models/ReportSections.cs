namespace Classic.Report.Models;

/// <summary>
/// Contains all sections that can be generated for a report.
/// </summary>
public class ReportSections
{
    // Common sections for all report formats
    public HeaderSection Header { get; set; } = new();
    public ErrorSection MainError { get; set; } = new();
    public List<SuspectSection> CrashSuspects { get; set; } = new();
    public SettingsValidationSection Settings { get; set; } = new();
    public List<PluginSection> PluginSuspects { get; set; } = new();
    public List<FormIdSection> FormIdSuspects { get; set; } = new();
    public List<NamedRecordSection> NamedRecords { get; set; } = new();
    public FooterSection Footer { get; set; } = new();

    // Enhanced/Advanced formatting sections
    public ExecutiveSummarySection? ExecutiveSummary { get; set; }
    public PerformanceMetricsSection? Performance { get; set; }
    public GameHintsSection? GameHints { get; set; }

    // FCX-specific sections (Advanced only)
    public FCXNoticeSection? FCXNotice { get; set; }
    public MainFilesCheckSection? MainFilesCheck { get; set; }
    public GameFilesCheckSection? GameFilesCheck { get; set; }
    public ExtendedPerformanceSection? ExtendedPerformance { get; set; }
}

/// <summary>
/// Base class for all report sections.
/// </summary>
public abstract class ReportSectionBase
{
    public string Title { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsEmpty { get; set; } = false;
}

/// <summary>
/// Header section containing basic report information.
/// </summary>
public class HeaderSection : ReportSectionBase
{
    public string FileName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; } = DateTime.Now;
    public bool FCXModeEnabled { get; set; } = false;
}

/// <summary>
/// Main error section containing crash details.
/// </summary>
public class ErrorSection : ReportSectionBase
{
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int SeverityScore { get; set; }
    public string? StackTrace { get; set; }
}

/// <summary>
/// Section for crash suspects.
/// </summary>
public class SuspectSection : ReportSectionBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SeverityScore { get; set; }
    public string? Evidence { get; set; }
    public string? Recommendation { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// Section for settings validation results.
/// </summary>
public class SettingsValidationSection : ReportSectionBase
{
    public bool AllSettingsValid { get; set; } = true;
    public List<SettingIssueSection> Issues { get; set; } = new();
}

/// <summary>
/// Section for individual setting issues.
/// </summary>
public class SettingIssueSection : ReportSectionBase
{
    public string SettingName { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public int SeverityScore { get; set; }
    public string? RecommendedValue { get; set; }
}

/// <summary>
/// Section for plugin information.
/// </summary>
public class PluginSection : ReportSectionBase
{
    public int LoadOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Flags { get; set; }
    public bool IsSuspicious { get; set; } = false;
}

/// <summary>
/// Section for FormID information.
/// </summary>
public class FormIdSection : ReportSectionBase
{
    public uint FormId { get; set; }
    public byte PluginIndex { get; set; }
    public uint LocalFormId { get; set; }
    public string PluginName { get; set; } = string.Empty;
    public string? FormType { get; set; }
}

/// <summary>
/// Section for named records.
/// </summary>
public class NamedRecordSection : ReportSectionBase
{
    public string RecordName { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public string PluginName { get; set; } = string.Empty;
    public uint FormId { get; set; }
}

/// <summary>
/// Footer section with generation information.
/// </summary>
public class FooterSection : ReportSectionBase
{
    public string GeneratorVersion { get; set; } = string.Empty;
    public DateTime GenerationDate { get; set; } = DateTime.Now;
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Executive summary section for enhanced reports.
/// </summary>
public class ExecutiveSummarySection : ReportSectionBase
{
    public string SummaryText { get; set; } = string.Empty;
    public int TotalSuspects { get; set; }
    public int CriticalIssues { get; set; }
    public int ModCompatibilityIssues { get; set; }
    public string RecommendedActions { get; set; } = string.Empty;
}

/// <summary>
/// Performance metrics section.
/// </summary>
public class PerformanceMetricsSection : ReportSectionBase
{
    public TimeSpan AnalysisTime { get; set; }
    public int FilesProcessed { get; set; }
    public long MemoryUsed { get; set; }
    public int PluginsAnalyzed { get; set; }
}

/// <summary>
/// Game hints section.
/// </summary>
public class GameHintsSection : ReportSectionBase
{
    public List<GameHint> Hints { get; set; } = new();
}

/// <summary>
/// Individual game hint.
/// </summary>
public class GameHint
{
    public string Category { get; set; } = string.Empty;
    public string HintText { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string? RelatedPlugin { get; set; }
}

/// <summary>
/// FCX notice section.
/// </summary>
public class FCXNoticeSection : ReportSectionBase
{
    public string NoticeText { get; set; } = string.Empty;
    public bool FCXEnabled { get; set; } = true;
}

/// <summary>
/// Main files check section for FCX mode.
/// </summary>
public class MainFilesCheckSection : ReportSectionBase
{
    public List<FileCheckResult> FileResults { get; set; } = new();
    public bool AllFilesValid { get; set; } = true;
}

/// <summary>
/// Game files check section for FCX mode.
/// </summary>
public class GameFilesCheckSection : ReportSectionBase
{
    public List<FileCheckResult> FileResults { get; set; } = new();
    public bool IntegrityCheckPassed { get; set; } = true;
    public int TotalFilesChecked { get; set; }
}

/// <summary>
/// Extended performance section for FCX mode.
/// </summary>
public class ExtendedPerformanceSection : ReportSectionBase
{
    public TimeSpan FileSystemCheckTime { get; set; }
    public int WorkerThreadsUsed { get; set; }
    public long IOOperations { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
}

/// <summary>
/// Result of a file check operation.
/// </summary>
public class FileCheckResult
{
    public string FilePath { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
    public string? IssueDescription { get; set; }
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
}
