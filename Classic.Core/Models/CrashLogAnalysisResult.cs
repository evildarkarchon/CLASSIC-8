namespace Classic.Core.Models;

/// <summary>
/// Represents the complete result of a crash log analysis.
/// </summary>
public class CrashLogAnalysisResult
{
    public CrashLog CrashLog { get; set; } = new();
    public ScanStatistics Statistics { get; set; } = new();
    public List<Suspect> CrashSuspects { get; set; } = new();
    public List<Plugin> ProblematicPlugins { get; set; } = new();
    public List<FormId> SuspectFormIds { get; set; } = new();
    public List<NamedRecord> NamedRecords { get; set; } = new();
    public SettingsValidationResult SettingsValidation { get; set; } = new();
    public PluginLimitStatus PluginLimitStatus { get; set; } = new();
    public List<ModCompatibilityIssue> ModCompatibilityIssues { get; set; } = new();
    public string ClassicVersion { get; set; } = "CLASSIC v1.0.0";
    public DateTime GeneratedDate { get; set; } = DateTime.Now;
    public string CrashGenName { get; set; } = "Buffout 4 NG";
    public Version LatestCrashGenVersion { get; set; } = new();
    public Version LatestCrashGenVrVersion { get; set; } = new();
    public bool IsOutdated => DetectedCrashGenVersion < LatestCrashGenVersion || DetectedCrashGenVersion < LatestCrashGenVrVersion;
    
    private Version? _detectedCrashGenVersion;
    public Version DetectedCrashGenVersion
    {
        get
        {
            if (_detectedCrashGenVersion == null && !string.IsNullOrEmpty(CrashLog.CrashGenVersion))
            {
                Version.TryParse(CrashLog.CrashGenVersion, out var version);
                _detectedCrashGenVersion = version ?? new Version();
            }
            return _detectedCrashGenVersion ?? new Version();
        }
        set => _detectedCrashGenVersion = value;
    }
}

public class SettingsValidationResult
{
    public bool AllSettingsValid { get; set; } = true;
    public List<SettingIssue> Issues { get; set; } = new();
}

public class SettingIssue
{
    public string SettingName { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SeverityLevel Severity { get; set; } = SeverityLevel.Low;
    public int SeverityScore { get; set; } = 2; // 1-6 scale
}

public class PluginLimitStatus
{
    public bool ReachedLimit { get; set; }
    public bool LimitCheckDisabled { get; set; }
    public bool PluginsLoaded { get; set; } = true;
    public int CurrentPluginCount { get; set; }
    public int MaxPluginCount { get; set; } = 254;
}

public class ModCompatibilityIssue
{
    public string ModName { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ConflictingMods { get; set; } = new();
    public string Resolution { get; set; } = string.Empty;
    public SeverityLevel Severity { get; set; } = SeverityLevel.Medium;
}

public class NamedRecord
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public FormId FormId { get; set; } = new();
    public Plugin OriginPlugin { get; set; } = new();
}