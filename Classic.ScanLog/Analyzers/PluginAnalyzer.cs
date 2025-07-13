using System.Text.RegularExpressions;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.ScanLog.Models;
using Classic.ScanLog.Parsers;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Analyzers;

/// <summary>
///     Analyzes plugin data from crash logs to identify issues and conflicts.
///     Implements the IPluginAnalyzer interface.
/// </summary>
public class PluginAnalyzer : IPluginAnalyzer
{
    private readonly ScanLogConfiguration _configuration;
    private readonly ILogger<PluginAnalyzer> _logger;
    private readonly PluginAnalysisConfiguration _pluginConfig;

    public PluginAnalyzer(
        ILogger<PluginAnalyzer> logger,
        ScanLogConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _pluginConfig = configuration.PluginAnalysis;
    }

    /// <summary>
    ///     Analyzes plugins from a crash log asynchronously
    /// </summary>
    public async Task<object> AnalyzePluginsAsync(CrashLog crashLog, CancellationToken cancellationToken = default)
    {
        return await AnalyzePluginsInternalAsync(crashLog, cancellationToken);
    }

    /// <summary>
    ///     Checks if a specific plugin exists in the crash log
    /// </summary>
    public async Task<bool> HasPluginAsync(CrashLog crashLog, string pluginName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!crashLog.HasValidPluginList())
                return false;

            var plugins = await ParsePluginListAsync(crashLog, cancellationToken);
            return plugins.Any(p => string.Equals(p.FileName, pluginName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for plugin {PluginName} in: {FileName}", pluginName,
                crashLog.FileName);
            return false;
        }
    }

    /// <summary>
    ///     Validates the plugin load order
    /// </summary>
    public async Task<List<string>> ValidateLoadOrderAsync(CrashLog crashLog,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();

        try
        {
            if (!crashLog.HasValidPluginList())
            {
                issues.Add("No plugin list available for load order validation");
                return issues;
            }

            var plugins = await ParsePluginListAsync(crashLog, cancellationToken);

            // Check if total exceeds limit
            if (plugins.Count > _pluginConfig.MaxPluginCount)
                issues.Add($"Plugin count ({plugins.Count}) exceeds maximum limit ({_pluginConfig.MaxPluginCount})");

            // Check if exceeds recommended limit
            if (plugins.Count > _pluginConfig.RecommendedMaxPlugins)
                issues.Add(
                    $"Plugin count ({plugins.Count}) exceeds recommended limit ({_pluginConfig.RecommendedMaxPlugins})");

            // Check for master files not being loaded first
            var masterFiles = plugins.Where(p => p.IsMaster).ToList();
            var nonMasterFiles = plugins.Where(p => !p.IsMaster).ToList();

            if (masterFiles.Any() && nonMasterFiles.Any())
            {
                var lastMasterIndex = plugins.Where(p => p.IsMaster).Max(p => p.LoadOrder);
                var firstNonMasterIndex = plugins.Where(p => !p.IsMaster).Min(p => p.LoadOrder);

                if (lastMasterIndex > firstNonMasterIndex)
                    issues.Add("Master files (.esm) should be loaded before plugin files (.esp/.esl)");
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate load order for: {FileName}", crashLog.FileName);
            issues.Add($"Error validating load order: {ex.Message}");
            return issues;
        }
    }

    /// <summary>
    ///     Internal implementation that returns the strongly typed result
    /// </summary>
    public async Task<PluginAnalysisResult> AnalyzePluginsInternalAsync(CrashLog crashLog,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting plugin analysis for: {FileName}", crashLog.FileName);

            var result = new PluginAnalysisResult();

            if (!crashLog.HasValidPluginList())
            {
                _logger.LogWarning("No valid plugin list found in crash log: {FileName}", crashLog.FileName);
                result.HasPluginList = false;
                return result;
            }

            result.HasPluginList = true;

            // Extract plugin counts
            var (light, regular, total) = crashLog.GetPluginCounts();
            result.LightPlugins = light;
            result.RegularPlugins = regular;
            result.TotalPlugins = total;
            result.ExceedsRecommendedLimit = total > _pluginConfig.RecommendedMaxPlugins;

            // Parse individual plugins
            var plugins = await ParsePluginListAsync(crashLog, cancellationToken);

            // Analyze for problematic plugins
            if (_pluginConfig.CheckForProblematicMods)
                result.ProblematicPlugins = await IdentifyProblematicPluginsAsync(plugins, cancellationToken);

            // Analyze for conflicts
            if (_pluginConfig.CheckForConflicts)
                result.ConflictingPlugins = await IdentifyConflictingPluginsAsync(plugins, cancellationToken);

            // Check for missing patches
            if (_pluginConfig.CheckForPatches)
                result.MissingPatches = await IdentifyMissingPatchesAsync(plugins, cancellationToken);

            _logger.LogDebug("Completed plugin analysis for: {FileName}", crashLog.FileName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze plugins for: {FileName}", crashLog.FileName);
            throw;
        }
    }

    /// <summary>
    ///     Parses the plugin list from crash log segments
    /// </summary>
    private async Task<List<Plugin>> ParsePluginListAsync(CrashLog crashLog, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var plugins = new List<Plugin>();

            if (!crashLog.Segments.TryGetValue("Plugins", out var pluginLines))
                return plugins;

            var pluginPattern = @"^\[([0-9A-F]{2})\]\s+(.+?)(?:\s+\[(.+?)\])?$";

            foreach (var line in pluginLines.Skip(1)) // Skip header line
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var match = Regex.Match(line.Trim(), pluginPattern);
                if (match.Success)
                {
                    var loadOrderHex = match.Groups[1].Value;
                    var fileName = match.Groups[2].Value.Trim();
                    var flags = match.Groups[3].Success ? match.Groups[3].Value : string.Empty;

                    var plugin = new Plugin
                    {
                        FileName = fileName,
                        LoadOrder = Convert.ToInt32(loadOrderHex, 16),
                        IsMaster = fileName.EndsWith(".esm", StringComparison.OrdinalIgnoreCase),
                        IsLight = flags.Contains("Light") ||
                                  fileName.EndsWith(".esl", StringComparison.OrdinalIgnoreCase),
                        Status = DeterminePluginStatus(fileName, flags),
                        Flags = flags
                    };

                    plugins.Add(plugin);
                }
            }

            return plugins.OrderBy(p => p.LoadOrder).ToList();
        }, cancellationToken);
    }

    /// <summary>
    ///     Identifies problematic plugins based on known database
    /// </summary>
    private async Task<List<string>> IdentifyProblematicPluginsAsync(List<Plugin> plugins,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var problematicPlugins = new List<string>();

            foreach (var plugin in plugins)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check against known problematic mods database
                if (_pluginConfig.ProblematicMods.TryGetValue(plugin.FileName, out var problematicMod))
                    problematicPlugins.Add($"{plugin.FileName}: {problematicMod.Issue}");

                // Check for plugins with known patterns
                if (IsKnownProblematicPlugin(plugin.FileName))
                    problematicPlugins.Add($"{plugin.FileName}: Known to cause stability issues");
            }

            return problematicPlugins;
        }, cancellationToken);
    }

    /// <summary>
    ///     Identifies conflicting plugins based on known conflicts database
    /// </summary>
    private async Task<List<string>> IdentifyConflictingPluginsAsync(List<Plugin> plugins,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var conflicts = new List<string>();
            var pluginNames = plugins.Select(p => p.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var conflict in _pluginConfig.ModConflicts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var foundConflictingMods = conflict.ConflictingMods
                    .Where(mod => pluginNames.Contains(mod))
                    .ToList();

                if (foundConflictingMods.Count > 1)
                    conflicts.Add(
                        $"Conflict detected: {string.Join(", ", foundConflictingMods)} - {conflict.Description}");
            }

            return conflicts;
        }, cancellationToken);
    }

    /// <summary>
    ///     Identifies missing community patches for installed mods
    /// </summary>
    private async Task<List<string>> IdentifyMissingPatchesAsync(List<Plugin> plugins,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var missingPatches = new List<string>();
            var pluginNames = plugins.Select(p => p.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var plugin in plugins)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_pluginConfig.CommunityPatches.TryGetValue(plugin.FileName, out var patch))
                    // Check if the patch is installed
                    if (!pluginNames.Contains(patch.PatchName))
                    {
                        var severity = patch.IsRequired ? "REQUIRED" : "RECOMMENDED";
                        missingPatches.Add(
                            $"{severity}: {patch.PatchName} for {plugin.FileName} - {patch.Description}");
                    }
            }

            return missingPatches;
        }, cancellationToken);
    }

    /// <summary>
    ///     Determines plugin status based on filename and flags
    /// </summary>
    private PluginStatus DeterminePluginStatus(string fileName, string flags)
    {
        if (flags.Contains("Light"))
            return PluginStatus.Light;

        if (fileName.EndsWith(".esm", StringComparison.OrdinalIgnoreCase))
            return PluginStatus.Master;

        if (fileName.EndsWith(".esl", StringComparison.OrdinalIgnoreCase))
            return PluginStatus.Light;

        if (fileName.EndsWith(".esp", StringComparison.OrdinalIgnoreCase))
            return PluginStatus.Regular;

        return PluginStatus.Unknown;
    }

    /// <summary>
    ///     Checks if a plugin is known to be problematic based on patterns
    /// </summary>
    private bool IsKnownProblematicPlugin(string fileName)
    {
        // Common patterns for problematic plugins
        var problematicPatterns = new[]
        {
            @".*unofficial.*patch.*", // Outdated unofficial patches
            @".*oldrim.*", // Oldrim/LE mods incorrectly installed
            @".*sse.*" // SSE mods incorrectly installed for FO4
        };

        return problematicPatterns.Any(pattern =>
            Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Scans for loadorder.txt file in the main folder and loads plugins from it.
    /// This overrides crash log plugin detection when present.
    /// </summary>
    /// <param name="mainFolderPath">Path to the main CLASSIC folder</param>
    /// <returns>Dictionary of plugin names with origin markers and whether plugins were loaded</returns>
    public async Task<(Dictionary<string, string> plugins, bool pluginsLoaded)> ScanLoadOrderTxtAsync(
        string mainFolderPath)
    {
        var loadOrderPath = Path.Combine(mainFolderPath, "loadorder.txt");
        var loadOrderPlugins = new Dictionary<string, string>();

        _logger.LogDebug("Checking for loadorder.txt at: {Path}", loadOrderPath);

        try
        {
            if (!File.Exists(loadOrderPath)) return (loadOrderPlugins, false);

            _logger.LogInformation("Found loadorder.txt file, will override crash log plugin detection");

            var lines = await File.ReadAllLinesAsync(loadOrderPath);

            // Skip header line (first line) if present
            var pluginLines = lines.Length > 1 ? lines.Skip(1) : lines;

            foreach (var line in pluginLines)
            {
                var pluginEntry = line.Trim();
                if (!string.IsNullOrEmpty(pluginEntry) && !loadOrderPlugins.ContainsKey(pluginEntry))
                    loadOrderPlugins[pluginEntry] = "LO"; // Origin marker for loadorder.txt
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading loadorder.txt: {Message}", ex.Message);
        }

        return (loadOrderPlugins, loadOrderPlugins.Count > 0);
    }

    /// <summary>
    /// Analyzes plugin matches in the call stack to identify plugins involved in the crash.
    /// </summary>
    /// <param name="crashLog">The crash log to analyze</param>
    /// <param name="plugins">List of known plugins</param>
    /// <returns>Dictionary of plugin names and their occurrence counts in the call stack</returns>
    public Dictionary<string, int> AnalyzePluginMatches(CrashLog crashLog, List<Plugin> plugins)
    {
        var pluginMatches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var pluginNames = new HashSet<string>(plugins.Select(p => p.FileName), StringComparer.OrdinalIgnoreCase);

        // Get call stack segments
        var callStackSegments = new[] { "PROBABLE CALL STACK", "STACK", "MODULES" };

        foreach (var segmentName in callStackSegments)
            if (crashLog.Segments.TryGetValue(segmentName, out var lines))
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Check if line contains any known plugin name
                    foreach (var pluginName in pluginNames)
                        if (line.Contains(pluginName, StringComparison.OrdinalIgnoreCase))
                            pluginMatches[pluginName] = pluginMatches.GetValueOrDefault(pluginName, 0) + 1;
                }

        return pluginMatches;
    }

    /// <summary>
    /// Filters out ignored plugins from the plugin list based on configuration.
    /// </summary>
    /// <param name="plugins">Dictionary of plugins to filter</param>
    /// <returns>Filtered dictionary with ignored plugins removed</returns>
    public Dictionary<string, string> FilterIgnoredPlugins(Dictionary<string, string> plugins)
    {
        if (!_configuration.IgnorePluginsList?.Any() == true) return plugins;

        var filteredPlugins = new Dictionary<string, string>(plugins, StringComparer.OrdinalIgnoreCase);
        var ignoreList = new HashSet<string>(_configuration.IgnorePluginsList, StringComparer.OrdinalIgnoreCase);

        var keysToRemove = filteredPlugins.Keys
            .Where(key => ignoreList.Contains(key))
            .ToList();

        foreach (var key in keysToRemove) filteredPlugins.Remove(key);

        return filteredPlugins;
    }

    /// <summary>
    /// Detects advanced plugin limit scenarios including FF prefix detection.
    /// </summary>
    /// <param name="plugins">List of plugins to analyze</param>
    /// <param name="gameVersion">Current game version</param>
    /// <returns>Plugin limit status information</returns>
    public PluginLimitStatus DetectPluginLimitStatus(List<Plugin> plugins, Version gameVersion)
    {
        var status = new PluginLimitStatus
        {
            CurrentPluginCount = plugins.Count,
            MaxPluginCount = 254
        };

        // Check for FF prefix (plugin limit reached indicator)
        var hasFFPrefix = plugins.Any(p => p.LoadOrder == 255); // FF in hex = 255 in decimal

        if (hasFFPrefix)
        {
            status.ReachedLimit = true;

            // Check if limit checking is disabled (this would be detected from settings)
            // For now, assume it's enabled unless explicitly disabled
            status.LimitCheckDisabled = false;
        }

        status.PluginsLoaded = plugins.Any();

        return status;
    }

    /// <summary>
    /// Generates a formatted report of plugin suspects found in the call stack.
    /// </summary>
    /// <param name="pluginMatches">Dictionary of plugins and their occurrence counts</param>
    /// <param name="crashGenName">Name of the crash generator</param>
    /// <returns>Formatted report string</returns>
    public string GeneratePluginSuspectsReport(Dictionary<string, int> pluginMatches, string crashGenName = "Buffout 4")
    {
        if (!pluginMatches.Any()) return "* COULDN'T FIND ANY PLUGIN SUSPECTS *";

        var report = new System.Text.StringBuilder();
        report.AppendLine("The following PLUGINS were found in the CRASH STACK:");

        // Sort by count (descending) then by name for consistent output
        var sortedMatches = pluginMatches
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key);

        foreach (var (plugin, count) in sortedMatches) report.AppendLine($"- {plugin} | {count}");

        report.AppendLine();
        report.AppendLine("[Last number counts how many times each Plugin Suspect shows up in the crash log.]");
        report.AppendLine(
            $"These Plugins were caught by {crashGenName} and some of them might be responsible for this crash.");
        report.AppendLine(
            "You can try disabling these plugins and check if the game still crashes, though this method can be unreliable.");

        return report.ToString();
    }
}
