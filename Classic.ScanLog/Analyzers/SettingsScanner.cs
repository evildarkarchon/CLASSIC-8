using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.ScanLog.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Classic.ScanLog.Analyzers;

/// <summary>
/// Scans and validates crash generator and mod settings for compatibility issues.
/// Ported from Python SettingsScanner implementation.
/// </summary>
public class SettingsScanner : ISettingsScanner
{
    private readonly ScanLogConfiguration _configuration;
    private readonly string _crashGenName;
    private readonly HashSet<string> _crashGenIgnoreSettings;

    public SettingsScanner(ScanLogConfiguration configuration)
    {
        _configuration = configuration;
        _crashGenName = "Buffout 4"; // Default, can be configured
        _crashGenIgnoreSettings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "F4EE", "WaitForDebugger", "Achievements", "InputSwitch", "AutoOpen",
            "PromptUpload", "MemoryManagerDebug", "BSTextureStreamerLocalHeap", "ArchiveLimit"
        };
    }

    /// <inheritdoc />
    public IEnumerable<Suspect> Scan(CrashLog crashLog)
    {
        var suspects = new List<Suspect>();
        var settingsIssues = ValidateSettings(crashLog);

        foreach (var issue in settingsIssues)
            suspects.Add(new Suspect
            {
                Name = issue.SettingName,
                Description = issue.Description,
                Type = SuspectType.Setting,
                SeverityScore = issue.SeverityScore,
                Evidence = issue.CurrentValue,
                Recommendation = issue.FixInstructions
            });

        return suspects;
    }

    /// <summary>
    /// Validates all settings and returns a list of issues found.
    /// </summary>
    /// <param name="crashLog">The crash log to analyze</param>
    /// <returns>List of setting validation issues</returns>
    public List<SettingValidationIssue> ValidateSettings(CrashLog crashLog)
    {
        var issues = new List<SettingValidationIssue>();
        var crashGenSettings = ExtractCrashGenSettings(crashLog);
        var xseModules = ExtractXseModules(crashLog);

        // Validate achievements settings
        ValidateAchievementsSettings(crashGenSettings, xseModules, issues);

        // Validate memory management settings
        ValidateMemoryManagementSettings(crashGenSettings, xseModules, issues);

        // Validate archive limit settings
        ValidateArchiveLimitSettings(crashGenSettings, issues);

        // Validate Looks Menu/F4EE settings
        ValidateLooksMenuSettings(crashGenSettings, xseModules, issues);

        // Check for disabled settings
        CheckDisabledSettings(crashGenSettings, issues);

        return issues;
    }

    /// <summary>
    /// Extracts crash generator settings from the crash log.
    /// </summary>
    /// <param name="crashLog">The crash log to parse</param>
    /// <returns>Dictionary of setting names and values</returns>
    private Dictionary<string, object> ExtractCrashGenSettings(CrashLog crashLog)
    {
        var settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Look for Buffout 4 TOML configuration in the crash log
        if (crashLog.Segments.TryGetValue("BUFFOUT 4 TOML", out var tomlLines) ||
            crashLog.Segments.TryGetValue("TOML", out tomlLines))
            ParseTomlSettings(tomlLines, settings);

        return settings;
    }

    /// <summary>
    /// Parses TOML configuration lines and extracts settings.
    /// </summary>
    /// <param name="tomlLines">Lines containing TOML configuration</param>
    /// <param name="settings">Dictionary to populate with settings</param>
    private void ParseTomlSettings(IEnumerable<string> tomlLines, Dictionary<string, object> settings)
    {
        var settingPattern = new Regex(@"^\s*([\w]+)\s*=\s*(.+)$", RegexOptions.Compiled);

        foreach (var line in tomlLines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            var match = settingPattern.Match(line);
            if (match.Success)
            {
                var key = match.Groups[1].Value.Trim();
                var value = match.Groups[2].Value.Trim();

                // Parse boolean values
                if (bool.TryParse(value, out var boolValue))
                    settings[key] = boolValue;
                // Parse integer values
                else if (int.TryParse(value, out var intValue))
                    settings[key] = intValue;
                // Keep as string
                else
                    settings[key] = value.Trim('"', '\'');
            }
        }
    }

    /// <summary>
    /// Extracts XSE (F4SE) modules from the crash log.
    /// </summary>
    /// <param name="crashLog">The crash log to parse</param>
    /// <returns>Set of loaded XSE module names</returns>
    private HashSet<string> ExtractXseModules(CrashLog crashLog)
    {
        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (crashLog.Segments.TryGetValue("F4SE PLUGINS", out var xseLines))
            foreach (var line in xseLines)
            {
                // Extract DLL names from XSE plugin lines
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                    if (part.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        modules.Add(part.ToLowerInvariant());
            }

        return modules;
    }

    /// <summary>
    /// Validates achievements-related settings.
    /// </summary>
    private void ValidateAchievementsSettings(Dictionary<string, object> settings, HashSet<string> xseModules,
        List<SettingValidationIssue> issues)
    {
        var achievementsEnabled = GetBooleanSetting(settings, "Achievements", false);
        var hasAchievementsMod =
            xseModules.Contains("achievements.dll") || xseModules.Contains("unlimitedsurvivalmode.dll");

        if (achievementsEnabled && hasAchievementsMod)
            issues.Add(new SettingValidationIssue
            {
                SettingName = "Achievements Configuration Conflict",
                Description =
                    "Achievements Mod and/or Unlimited Survival Mode is installed, but Achievements parameter is set to TRUE",
                CurrentValue = "TRUE",
                ExpectedValue = "FALSE",
                SeverityScore = 4, // High severity
                FixInstructions =
                    $"Open {_crashGenName}'s TOML file and change Achievements to FALSE, this prevents conflicts with {_crashGenName}."
            });
    }

    /// <summary>
    /// Validates memory management settings.
    /// </summary>
    private void ValidateMemoryManagementSettings(Dictionary<string, object> settings, HashSet<string> xseModules,
        List<SettingValidationIssue> issues)
    {
        var memoryManagerEnabled = GetBooleanSetting(settings, "MemoryManager", false);
        var hasXCell = xseModules.Contains("xcell.dll");
        var hasBakaScrapHeap = xseModules.Contains("bakascrapheap.dll");

        if (memoryManagerEnabled && hasXCell)
            issues.Add(new SettingValidationIssue
            {
                SettingName = "MemoryManager X-Cell Conflict",
                Description = "X-Cell is installed, but MemoryManager parameter is set to TRUE",
                CurrentValue = "TRUE",
                ExpectedValue = "FALSE",
                SeverityScore = 4, // High severity
                FixInstructions =
                    $"Open {_crashGenName}'s TOML file and change MemoryManager to FALSE, this prevents conflicts with X-Cell."
            });
        else if (memoryManagerEnabled && hasBakaScrapHeap)
            issues.Add(new SettingValidationIssue
            {
                SettingName = "Baka ScrapHeap Redundancy",
                Description = $"Baka ScrapHeap Mod is installed, but is redundant with {_crashGenName}",
                CurrentValue = "Installed",
                ExpectedValue = "Not Installed",
                SeverityScore = 3, // Medium severity
                FixInstructions = $"Uninstall the Baka ScrapHeap Mod, this prevents conflicts with {_crashGenName}."
            });

        // Check additional memory settings for X-Cell compatibility
        if (hasXCell)
        {
            var memorySettings = new Dictionary<string, string>
            {
                { "HavokMemorySystem", "Havok Memory System" },
                { "BSTextureStreamerLocalHeap", "BSTextureStreamerLocalHeap" },
                { "ScaleformAllocator", "Scaleform Allocator" },
                { "SmallBlockAllocator", "Small Block Allocator" }
            };

            foreach (var (settingKey, displayName) in memorySettings)
                if (GetBooleanSetting(settings, settingKey, false))
                    issues.Add(new SettingValidationIssue
                    {
                        SettingName = $"{settingKey} X-Cell Conflict",
                        Description = $"X-Cell is installed, but {settingKey} parameter is set to TRUE",
                        CurrentValue = "TRUE",
                        ExpectedValue = "FALSE",
                        SeverityScore = 4, // High severity
                        FixInstructions =
                            $"Open {_crashGenName}'s TOML file and change {settingKey} to FALSE, this prevents conflicts with X-Cell."
                    });
        }
    }

    /// <summary>
    /// Validates archive limit settings.
    /// </summary>
    private void ValidateArchiveLimitSettings(Dictionary<string, object> settings, List<SettingValidationIssue> issues)
    {
        var archiveLimitEnabled = GetBooleanSetting(settings, "ArchiveLimit", false);

        if (archiveLimitEnabled)
            issues.Add(new SettingValidationIssue
            {
                SettingName = "ArchiveLimit Instability",
                Description = "ArchiveLimit is set to TRUE, this setting is known to cause instability",
                CurrentValue = "TRUE",
                ExpectedValue = "FALSE",
                SeverityScore = 4, // High severity
                FixInstructions = $"Open {_crashGenName}'s TOML file and change ArchiveLimit to FALSE."
            });
    }

    /// <summary>
    /// Validates Looks Menu/F4EE settings.
    /// </summary>
    private void ValidateLooksMenuSettings(Dictionary<string, object> settings, HashSet<string> xseModules,
        List<SettingValidationIssue> issues)
    {
        var f4eeEnabled = GetBooleanSetting(settings, "F4EE", false);
        var hasLooksMenu = xseModules.Contains("f4ee.dll");

        if (!f4eeEnabled && hasLooksMenu)
            issues.Add(new SettingValidationIssue
            {
                SettingName = "Looks Menu F4EE Configuration",
                Description = "Looks Menu is installed, but F4EE parameter under [Compatibility] is set to FALSE",
                CurrentValue = "FALSE",
                ExpectedValue = "TRUE",
                SeverityScore = 4, // High severity
                FixInstructions =
                    $"Open {_crashGenName}'s TOML file and change F4EE to TRUE, this prevents bugs and crashes from Looks Menu."
            });
    }

    /// <summary>
    /// Checks for disabled settings that might be problematic.
    /// </summary>
    private void CheckDisabledSettings(Dictionary<string, object> settings, List<SettingValidationIssue> issues)
    {
        foreach (var (settingName, settingValue) in settings)
            if (settingValue is false && !_crashGenIgnoreSettings.Contains(settingName))
                issues.Add(new SettingValidationIssue
                {
                    SettingName = $"Disabled Setting: {settingName}",
                    Description = $"{settingName} is disabled in your {_crashGenName} settings, is this intentional?",
                    CurrentValue = "FALSE",
                    ExpectedValue = "Review if intentional",
                    SeverityScore = 1, // Info level
                    FixInstructions = $"Review if {settingName} should be enabled in {_crashGenName} settings."
                });
    }

    /// <summary>
    /// Gets a boolean setting value from the settings dictionary.
    /// </summary>
    /// <param name="settings">Settings dictionary</param>
    /// <param name="key">Setting key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Boolean value</returns>
    private bool GetBooleanSetting(Dictionary<string, object> settings, string key, bool defaultValue)
    {
        if (settings.TryGetValue(key, out var value))
            return value switch
            {
                bool boolValue => boolValue,
                string stringValue => bool.TryParse(stringValue, out var parsed) ? parsed : defaultValue,
                _ => defaultValue
            };
        return defaultValue;
    }
}

/// <summary>
/// Represents a setting validation issue found during scanning.
/// </summary>
public class SettingValidationIssue
{
    public string SettingName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public int SeverityScore { get; set; } = 3;
    public string FixInstructions { get; set; } = string.Empty;
}
