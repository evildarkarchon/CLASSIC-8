using Microsoft.Extensions.Logging;
using Classic.ScanLog.Configuration;
using Classic.ScanLog.Models;
using Classic.ScanLog.Utilities;
using Classic.Core.Models;
using Classic.Core.Interfaces;

namespace Classic.ScanLog.Analyzers;

/// <summary>
/// Detects mod conflicts and compatibility issues
/// Ported from Python CLASSIC DetectMods functionality
/// </summary>
public class ModConflictDetector
{
    private readonly ILogger<ModConflictDetector> _logger;
    private readonly ModDatabaseLoader _databaseLoader;
    private readonly IGpuDetector _gpuDetector;
    private ModConflictDatabase? _database;

    public ModConflictDetector(
        ILogger<ModConflictDetector> logger,
        ModDatabaseLoader databaseLoader,
        IGpuDetector gpuDetector)
    {
        _logger = logger;
        _databaseLoader = databaseLoader;
        _gpuDetector = gpuDetector;
    }

    /// <summary>
    /// Initializes the mod conflict detector by loading the database
    /// </summary>
    public async Task InitializeAsync()
    {
        _database = await _databaseLoader.LoadModDatabaseAsync();
        _logger.LogInformation("ModConflictDetector initialized with {TotalMods} mod entries", 
            GetTotalModCount());
    }

    /// <summary>
    /// Performs comprehensive mod conflict detection on crash log data
    /// </summary>
    /// <param name="crashLogData">Parsed crash log data</param>
    /// <returns>List of detected mod conflicts</returns>
    public async Task<List<ModConflictResult>> DetectModConflictsAsync(CrashLogData crashLogData)
    {
        if (_database == null)
        {
            await InitializeAsync();
        }

        var conflicts = new List<ModConflictResult>();
        var gpuInfo = (_gpuDetector as GpuDetector)?.GetGpuInfo(crashLogData.SystemSpecs ?? string.Empty) ?? new GpuInfo();

        _logger.LogDebug("Starting mod conflict detection for {PluginCount} plugins", 
            crashLogData.Plugins?.Count ?? 0);

        try
        {
            // 1. Detect frequent crash mods (Mods_FREQ)
            var frequentCrashConflicts = DetectFrequentCrashMods(crashLogData.Plugins);
            conflicts.AddRange(frequentCrashConflicts);

            // 2. Detect mod pair conflicts (Mods_CONF) 
            var pairConflicts = DetectModPairConflicts(crashLogData.Plugins);
            conflicts.AddRange(pairConflicts);

            // 3. Detect missing important mods (Mods_CORE)
            var importantModConflicts = DetectImportantMods(crashLogData.Plugins, gpuInfo);
            conflicts.AddRange(importantModConflicts);

            // 4. Detect mods with solutions (Mods_SOLU)
            var solutionConflicts = DetectModsWithSolutions(crashLogData.Plugins);
            conflicts.AddRange(solutionConflicts);

            // 5. Check load order issues
            var loadOrderConflicts = DetectLoadOrderIssues(crashLogData.Plugins);
            conflicts.AddRange(loadOrderConflicts);

            _logger.LogInformation("Mod conflict detection completed. Found {ConflictCount} issues", 
                conflicts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during mod conflict detection");
        }

        return conflicts;
    }

    /// <summary>
    /// Detects mods that frequently cause crashes (Python detect_mods_single equivalent)
    /// </summary>
    private List<ModConflictResult> DetectFrequentCrashMods(List<PluginInfo>? plugins)
    {
        var conflicts = new List<ModConflictResult>();

        if (_database?.ModsFreq == null || plugins == null)
            return conflicts;

        foreach (var plugin in plugins)
        {
            foreach (var (modPattern, warning) in _database.ModsFreq)
            {
                if (IsModNameMatch(plugin.FileName, modPattern) || 
                    IsModNameMatch(plugin.DisplayName, modPattern))
                {
                    conflicts.Add(new ModConflictResult
                    {
                        ModName = modPattern,
                        PluginId = plugin.FileName,
                        Warning = warning,
                        Severity = ConflictSeverity.Critical,
                        Type = ConflictType.FrequentCrash
                    });

                    _logger.LogWarning("Found frequent crash mod: {ModName} [{PluginId}]", 
                        modPattern, plugin.FileName);
                    break; // Only report once per plugin
                }
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects conflicting mod pairs (Python detect_mods_double equivalent)
    /// </summary>
    private List<ModConflictResult> DetectModPairConflicts(List<PluginInfo>? plugins)
    {
        var conflicts = new List<ModConflictResult>();

        if (_database?.ModsConf == null || plugins == null)
            return conflicts;

        var installedMods = plugins.Select(p => p.FileName.ToLowerInvariant()).ToHashSet();

        foreach (var (conflictPair, warning) in _database.ModsConf)
        {
            // Parse mod pairs using " | " separator
            var modParts = conflictPair.Split(" | ", StringSplitOptions.RemoveEmptyEntries);
            if (modParts.Length != 2) continue;

            var mod1 = modParts[0].Trim();
            var mod2 = modParts[1].Trim();

            // Check if both mods are present
            var hasMod1 = installedMods.Any(m => IsModNameMatch(m, mod1));
            var hasMod2 = installedMods.Any(m => IsModNameMatch(m, mod2));

            if (hasMod1 && hasMod2)
            {
                conflicts.Add(new ModConflictResult
                {
                    ModName = conflictPair,
                    PluginId = $"{mod1} + {mod2}",
                    Warning = warning,
                    Severity = ConflictSeverity.Caution,
                    Type = ConflictType.ModPairConflict
                });

                _logger.LogWarning("Found mod pair conflict: {Mod1} conflicts with {Mod2}", 
                    mod1, mod2);
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects missing important mods and GPU-specific issues (Python detect_mods_important equivalent)
    /// </summary>
    private List<ModConflictResult> DetectImportantMods(List<PluginInfo>? plugins, GpuInfo gpuInfo)
    {
        var conflicts = new List<ModConflictResult>();

        if (_database?.ModsCore == null)
            return conflicts;

        var installedMods = plugins?.Select(p => p.FileName.ToLowerInvariant()).ToHashSet() ?? new HashSet<string>();

        foreach (var (modEntry, description) in _database.ModsCore)
        {
            // Parse mod entries with " | " separator (mod_id | display_name)
            var modParts = modEntry.Split(" | ", StringSplitOptions.RemoveEmptyEntries);
            var modId = modParts[0].Trim();
            var displayName = modParts.Length > 1 ? modParts[1].Trim() : modId;

            var isInstalled = installedMods.Any(m => IsModNameMatch(m, modId));

            if (!isInstalled)
            {
                // Check for GPU-specific mods
                var gpuDetector = _gpuDetector as GpuDetector;
                var gpuWarning = gpuDetector?.GetGpuCompatibilityWarning(modId, gpuInfo);
                var severity = (gpuDetector?.IsModGpuSpecific(modId, gpuInfo) ?? false)
                    ? ConflictSeverity.Critical 
                    : ConflictSeverity.Warning;

                conflicts.Add(new ModConflictResult
                {
                    ModName = displayName,
                    PluginId = modId,
                    Warning = gpuWarning ?? $"❓ Important mod not detected: {description}",
                    Severity = severity,
                    Type = ConflictType.MissingImportant,
                    GpuSpecific = gpuWarning
                });

                _logger.LogInformation("Important mod not detected: {ModName} [{ModId}]", 
                    displayName, modId);
            }
            else
            {
                _logger.LogDebug("✔️ Important mod detected: {ModName}", displayName);
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects mods that have available solutions or patches
    /// </summary>
    private List<ModConflictResult> DetectModsWithSolutions(List<PluginInfo>? plugins)
    {
        var conflicts = new List<ModConflictResult>();

        if (_database?.ModsSolu == null || plugins == null)
            return conflicts;

        foreach (var plugin in plugins)
        {
            foreach (var (modPattern, solution) in _database.ModsSolu)
            {
                if (IsModNameMatch(plugin.FileName, modPattern) || 
                    IsModNameMatch(plugin.DisplayName, modPattern))
                {
                    conflicts.Add(new ModConflictResult
                    {
                        ModName = modPattern,
                        PluginId = plugin.FileName,
                        Warning = solution,
                        Severity = ConflictSeverity.Info,
                        Type = ConflictType.HasSolution
                    });

                    _logger.LogInformation("Found mod with available solution: {ModName} [{PluginId}]", 
                        modPattern, plugin.FileName);
                    break;
                }
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Detects load order issues like plugin limits
    /// </summary>
    private List<ModConflictResult> DetectLoadOrderIssues(List<PluginInfo>? plugins)
    {
        var conflicts = new List<ModConflictResult>();

        if (plugins == null) return conflicts;

        // Check for plugin limit (255+ plugins)
        if (plugins.Any(p => p.HasPluginLimit))
        {
            conflicts.Add(new ModConflictResult
            {
                ModName = "Plugin Limit",
                PluginId = "[FF] marker detected",
                Warning = _database?.LoadOrderWarnings?.GetValueOrDefault("plugin_limit") ?? 
                         "Plugin limit exceeded (255+ plugins detected)",
                Severity = ConflictSeverity.Critical,
                Type = ConflictType.LoadOrderIssue
            });

            _logger.LogWarning("Plugin limit exceeded: {PluginCount}+ plugins detected", 
                plugins.Count);
        }

        return conflicts;
    }

    /// <summary>
    /// Performs case-insensitive mod name matching with substring support
    /// </summary>
    private static bool IsModNameMatch(string pluginName, string modPattern)
    {
        if (string.IsNullOrEmpty(pluginName) || string.IsNullOrEmpty(modPattern))
            return false;

        return pluginName.ToLowerInvariant().Contains(modPattern.ToLowerInvariant());
    }

    /// <summary>
    /// Gets the total number of mod entries in the database
    /// </summary>
    private int GetTotalModCount()
    {
        if (_database == null) return 0;

        return (_database.ModsCore?.Count ?? 0) +
               (_database.ModsFreq?.Count ?? 0) +
               (_database.ModsConf?.Count ?? 0) +
               (_database.ModsSolu?.Count ?? 0);
    }
}

/// <summary>
/// Crash log data structure for mod conflict detection
/// </summary>
public class CrashLogData
{
    public string? MainError { get; set; }
    public string? SystemSpecs { get; set; }
    public string? CallStack { get; set; }
    public List<PluginInfo>? Plugins { get; set; }
    public Dictionary<string, object>? Headers { get; set; }
}