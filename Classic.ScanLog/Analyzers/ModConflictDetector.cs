using Microsoft.Extensions.Logging;
using Classic.ScanLog.Configuration;
using Classic.ScanLog.Models;
using Classic.ScanLog.Utilities;
using Classic.Core.Models;
using Classic.Core.Interfaces;
using YamlDotNet.Serialization;

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
            foreach (var (modPattern, entryValue) in _database.ModsFreq)
            {
                var (warning, gpuConstraint) = ParseModEntry(entryValue);
                
                if (IsModNameMatch(plugin.FileName, modPattern) || 
                    IsModNameMatch(plugin.DisplayName, modPattern))
                {
                    conflicts.Add(new ModConflictResult
                    {
                        ModName = modPattern,
                        PluginId = plugin.FileName,
                        Warning = warning,
                        Solution = ExtractSolutionFromDescription(warning),
                        Severity = ConflictSeverity.Critical,
                        Type = ConflictType.FrequentCrash,
                        GpuSpecific = gpuConstraint
                    });

                    _logger.LogWarning("Found frequent crash mod: {ModName} [{PluginId}] (GPU: {Constraint})", 
                        modPattern, plugin.FileName, gpuConstraint ?? "Any");
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

        foreach (var (conflictPair, entryValue) in _database.ModsConf)
        {
            var (warning, gpuConstraint) = ParseModEntry(entryValue);
            
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
                    Solution = ExtractSolutionFromDescription(warning),
                    Severity = ConflictSeverity.Caution,
                    Type = ConflictType.ModPairConflict,
                    GpuSpecific = gpuConstraint
                });

                _logger.LogWarning("Found mod pair conflict: {Mod1} conflicts with {Mod2} (GPU: {Constraint})", 
                    mod1, mod2, gpuConstraint ?? "Any");
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

        foreach (var (modEntry, entryValue) in _database.ModsCore)
        {
            // Parse the entry value (can be string or object with GPU constraints)
            var (description, gpuConstraint) = ParseModEntry(entryValue);
            
            // Skip if GPU constraint doesn't match current system
            if (!ShouldProcessModForGpu(gpuConstraint, gpuInfo))
            {
                _logger.LogDebug("Skipping {ModEntry} due to GPU constraint: {Constraint}", 
                    modEntry, gpuConstraint);
                continue;
            }

            // Parse mod entries with " | " separator (mod_id | display_name)
            var modParts = modEntry.Split(" | ", StringSplitOptions.RemoveEmptyEntries);
            var modId = modParts[0].Trim();
            var displayName = modParts.Length > 1 ? modParts[1].Trim() : modId;

            var isInstalled = installedMods.Any(m => IsModNameMatch(m, modId));

            if (!isInstalled)
            {
                // Determine severity based on GPU constraint
                var severity = !string.IsNullOrEmpty(gpuConstraint)
                    ? ConflictSeverity.Critical  // GPU-specific mods are critical
                    : ConflictSeverity.Warning;

                var warningMessage = !string.IsNullOrEmpty(gpuConstraint)
                    ? $"❓ Important {gpuConstraint.ToUpperInvariant()}-specific mod not detected: {description}"
                    : $"❓ Important mod not detected: {description}";

                conflicts.Add(new ModConflictResult
                {
                    ModName = displayName,
                    PluginId = modId,
                    Warning = warningMessage,
                    Solution = ExtractSolutionFromDescription(description),
                    Severity = severity,
                    Type = ConflictType.MissingImportant,
                    GpuSpecific = gpuConstraint
                });

                _logger.LogInformation("Important mod not detected: {ModName} [{ModId}] (GPU: {Constraint})", 
                    displayName, modId, gpuConstraint ?? "Any");
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
            foreach (var (modPattern, entryValue) in _database.ModsSolu)
            {
                var (solution, gpuConstraint) = ParseModEntry(entryValue);
                
                if (IsModNameMatch(plugin.FileName, modPattern) || 
                    IsModNameMatch(plugin.DisplayName, modPattern))
                {
                    conflicts.Add(new ModConflictResult
                    {
                        ModName = modPattern,
                        PluginId = plugin.FileName,
                        Warning = solution,
                        Solution = ExtractSolutionFromDescription(solution),
                        Severity = ConflictSeverity.Info,
                        Type = ConflictType.HasSolution,
                        GpuSpecific = gpuConstraint
                    });

                    _logger.LogInformation("Found mod with available solution: {ModName} [{PluginId}] (GPU: {Constraint})", 
                        modPattern, plugin.FileName, gpuConstraint ?? "Any");
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
    /// Parses a mod entry which can be either a string or an object with GPU constraints
    /// </summary>
    private (string description, string? gpuConstraint) ParseModEntry(object entryValue)
    {
        try
        {
            // If it's a string, return it directly
            if (entryValue is string stringValue)
            {
                return (stringValue, null);
            }

            // If it's a dictionary/object, try to parse it as ModEntry
            if (entryValue is Dictionary<object, object> dictValue)
            {
                var description = dictValue.ContainsKey("description") 
                    ? dictValue["description"]?.ToString() ?? string.Empty
                    : string.Empty;
                var gpuConstraint = dictValue.ContainsKey("gpu_constraint")
                    ? dictValue["gpu_constraint"]?.ToString()
                    : null;

                return (description, gpuConstraint);
            }

            // Fallback to string representation
            return (entryValue?.ToString() ?? string.Empty, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse mod entry: {Entry}", entryValue);
            return (entryValue?.ToString() ?? string.Empty, null);
        }
    }

    /// <summary>
    /// Checks if a mod should be processed based on GPU constraints
    /// Supports inverted constraints with exclamation point prefix (!amd, !nvidia, etc.)
    /// </summary>
    private bool ShouldProcessModForGpu(string? gpuConstraint, GpuInfo gpuInfo)
    {
        if (string.IsNullOrEmpty(gpuConstraint))
            return true; // No constraint, always process

        var constraint = gpuConstraint.ToLowerInvariant().Trim();
        var manufacturer = gpuInfo.Manufacturer;
        
        // Check for inverted constraint (starts with !)
        if (constraint.StartsWith("!"))
        {
            var invertedConstraint = constraint.Substring(1); // Remove the !
            var matchesConstraint = invertedConstraint switch
            {
                "nvidia" => manufacturer == GpuManufacturer.Nvidia,
                "amd" => manufacturer == GpuManufacturer.Amd,
                "intel" => manufacturer == GpuManufacturer.Intel,
                _ => false // Unknown constraint, assume no match
            };
            return !matchesConstraint; // Invert the result
        }

        // Normal constraint matching
        return constraint switch
        {
            "nvidia" => manufacturer == GpuManufacturer.Nvidia,
            "amd" => manufacturer == GpuManufacturer.Amd,
            "intel" => manufacturer == GpuManufacturer.Intel,
            _ => true // Unknown constraint, process anyway
        };
    }

    /// <summary>
    /// Extracts solution/link information from description text
    /// </summary>
    private string ExtractSolutionFromDescription(string description)
    {
        if (description.Contains("Link:", StringComparison.OrdinalIgnoreCase))
        {
            var linkIndex = description.IndexOf("Link:", StringComparison.OrdinalIgnoreCase);
            var linkPart = description.Substring(linkIndex);
            var endIndex = linkPart.IndexOf('\n');
            return endIndex > 0 ? linkPart.Substring(0, endIndex).Trim() : linkPart.Trim();
        }
        return string.Empty;
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