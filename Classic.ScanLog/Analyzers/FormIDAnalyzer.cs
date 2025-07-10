using System.Globalization;
using System.Text.RegularExpressions;
using System.Data.SQLite;
using System.Collections.Concurrent;
using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.ScanLog.Models;
using Classic.ScanLog.Parsers;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Analyzers;

/// <summary>
///     Analyzes FormIDs from crash logs to identify problematic records and mod conflicts.
///     Implements the IFormIDAnalyzer interface.
/// </summary>
public class FormIdAnalyzer : IFormIdAnalyzer
{
    private readonly ScanLogConfiguration _configuration;
    private readonly ILogger<FormIdAnalyzer> _logger;
    private readonly bool _showFormIdValues;
    private readonly bool _formIdDbExists;
    private readonly List<string> _databasePaths;
    private readonly ConcurrentDictionary<(string formId, string plugin), string> _queryCache;

    public FormIdAnalyzer(
        ILogger<FormIdAnalyzer> logger,
        ScanLogConfiguration configuration,
        bool showFormIdValues = false,
        IEnumerable<string>? databasePaths = null)
    {
        _logger = logger;
        _configuration = configuration;
        _showFormIdValues = showFormIdValues;
        _databasePaths = databasePaths?.ToList() ?? new List<string>();
        _formIdDbExists = _databasePaths.Any(path => File.Exists(path));
        _queryCache = new ConcurrentDictionary<(string, string), string>();
        
        if (_formIdDbExists)
        {
            _logger.LogInformation("FormID database found, enabling database lookups");
        }
        else
        {
            _logger.LogDebug("No FormID database found, database lookups disabled");
        }
    }

    /// <summary>
    ///     Analyzes FormIDs from a crash log asynchronously
    /// </summary>
    public async Task<List<FormId>> AnalyzeFormIDsAsync(CrashLog crashLog,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting FormID analysis for: {FileName}", crashLog.FileName);

            var formIDs = new List<FormId>();

            // Extract FormIDs from all segments
            foreach (var segment in crashLog.Segments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var segmentFormIDs = await ExtractFormIDsFromSegmentAsync(
                    segment.Key,
                    segment.Value,
                    cancellationToken);

                formIDs.AddRange(segmentFormIDs);
            }

            // Deduplicate FormIDs
            var uniqueFormIDs = formIDs
                .GroupBy(f => new { FormIDValue = f.FormIdValue, f.SourcePlugin })
                .Select(g => g.First())
                .ToList();

            _logger.LogDebug("Found {Count} unique FormIDs in: {FileName}", uniqueFormIDs.Count, crashLog.FileName);
            return uniqueFormIDs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze FormIDs for: {FileName}", crashLog.FileName);
            throw;
        }
    }

    /// <summary>
    ///     Resolves FormID information including source plugin and record type
    /// </summary>
    public async Task<FormId> ResolveFormIdAsync(string formIdString, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var formId = new FormId();

            try
            {
                // Parse FormID value
                formId.FormIdValue = Convert.ToUInt32(formIdString, 16);

                // Extract plugin index from FormID
                var pluginIndex = (formId.FormIdValue >> 24) & 0xFF;
                formId.PluginIndex = (byte)pluginIndex;

                // Extract local FormID (within the plugin)
                formId.LocalFormId = formId.FormIdValue & 0x00FFFFFF;

                // Determine if it's a master file record
                formId.IsMasterRecord = pluginIndex < 0x0F; // First 15 slots are typically masters

                _logger.LogTrace("Resolved FormID {FormID}: Plugin={PluginIndex:X2}, Local={LocalFormID:X6}",
                    formId.FormIdValue, formId.PluginIndex, formId.LocalFormId);

                return formId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve FormID: {FormIDString}", formIdString);
                return formId;
            }
        }, cancellationToken);
    }

    /// <summary>
    ///     Validates FormID references against the plugin list
    /// </summary>
    public Task<List<string>> ValidateFormIDsAsync(CrashLog crashLog, List<FormId> formIDs,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();

        try
        {
            // Get plugin list for reference
            if (!crashLog.HasValidPluginList())
            {
                issues.Add("Cannot validate FormIDs: No plugin list available");
                return Task.FromResult(issues);
            }

            var (light, regular, total) = crashLog.GetPluginCounts();
            var maxValidIndex = total - 1;

            foreach (var formId in formIDs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if plugin index is valid
                if (formId.PluginIndex > maxValidIndex)
                    issues.Add(
                        $"FormID {formId.FormIdValue:X8} references invalid plugin index {formId.PluginIndex:X2} (max: {maxValidIndex:X2})");

                // Check for NULL references
                if (formId.FormIdValue == 0) issues.Add("NULL FormID reference detected - potential cause of crash");

                // Check for reserved ranges
                if (IsReservedFormId(formId.FormIdValue))
                    issues.Add($"FormID {formId.FormIdValue:X8} is in reserved range - may indicate corruption");

                // Check for common problematic FormIDs
                if (IsProblematicFormId(formId.FormIdValue))
                    issues.Add($"FormID {formId.FormIdValue:X8} is known to be problematic");
            }

            return Task.FromResult(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate FormIDs for: {FileName}", crashLog.FileName);
            issues.Add($"Error validating FormIDs: {ex.Message}");
            return Task.FromResult(issues);
        }
    }

    /// <summary>
    ///     Extracts FormIDs from a specific crash log segment
    /// </summary>
    private async Task<List<FormId>> ExtractFormIDsFromSegmentAsync(
        string segmentName,
        List<string> segmentLines,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var formIDs = new List<FormId>();

            var formIdPattern = _configuration.Patterns.FormIdPattern;
            var formTypePattern = _configuration.Patterns.FormTypePattern;

            foreach (var line in segmentLines)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Look for FormID patterns
                var formIdMatches = Regex.Matches(line, formIdPattern);
                foreach (Match match in formIdMatches)
                {
                    var formIdString = match.Groups[1].Value;
                    var formId = ResolveFormIdAsync(formIdString, cancellationToken).Result;

                    formId.SourceSegment = segmentName;
                    formId.Context = line.Trim();

                    // Try to extract additional information from the same line
                    ExtractAdditionalFormIdInfo(formId, line);

                    formIDs.Add(formId);
                }

                // Look for specific patterns in call stack that might indicate problematic FormIDs
                if (segmentName == "CallStack" && ContainsFormIdReference(line))
                {
                    var callStackFormId = ExtractFormIdFromCallStack(line);
                    if (callStackFormId != null)
                    {
                        callStackFormId.SourceSegment = segmentName;
                        callStackFormId.Context = line.Trim();
                        formIDs.Add(callStackFormId);
                    }
                }
            }

            return formIDs;
        }, cancellationToken);
    }

    /// <summary>
    ///     Extracts additional FormID information from the context line
    /// </summary>
    private void ExtractAdditionalFormIdInfo(FormId formId, string line)
    {
        try
        {
            // Extract form type if present
            var formTypeMatch = Regex.Match(line, _configuration.Patterns.FormTypePattern);
            if (formTypeMatch.Success)
            {
                formId.FormType = formTypeMatch.Groups[1].Value;
                if (int.TryParse(formTypeMatch.Groups[2].Value, out var typeId)) formId.FormTypeId = typeId;
            }

            // Extract plugin name if present in format: File: "pluginname.esp"
            var fileMatch = Regex.Match(line, @"File:\s*""([^""]+)""");
            if (fileMatch.Success) formId.SourcePlugin = fileMatch.Groups[1].Value;

            // Extract flags if present
            var flagsMatch = Regex.Match(line, @"Flags:\s*0x([0-9A-F]+)");
            if (flagsMatch.Success)
                if (uint.TryParse(flagsMatch.Groups[1].Value, NumberStyles.HexNumber, null,
                        out var flags))
                    formId.Flags = flags;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Failed to extract additional FormID info from line: {Line}", line);
        }
    }

    /// <summary>
    ///     Checks if a line contains FormID references in call stack format
    /// </summary>
    private bool ContainsFormIdReference(string line)
    {
        // Look for patterns like "-> 1447818+0x3A" which might contain FormID references
        return Regex.IsMatch(line, @"->\s*\d+\+0x[0-9A-F]+") ||
               Regex.IsMatch(line, "0x[0-9A-F]{8}") ||
               line.Contains("FormID") ||
               line.Contains("kNAVM") ||
               line.Contains("kREFR");
    }

    /// <summary>
    ///     Extracts FormID from call stack line
    /// </summary>
    private FormId? ExtractFormIdFromCallStack(string line)
    {
        try
        {
            // Look for hex patterns that might be FormIDs
            var hexMatches = Regex.Matches(line, "0x([0-9A-F]{8})");
            foreach (Match match in hexMatches)
            {
                var hexValue = match.Groups[1].Value;
                if (uint.TryParse(hexValue, NumberStyles.HexNumber, null, out var value))
                    // Simple heuristic: if it looks like a FormID (not too high, not zero)
                    if (value is > 0 and < 0xFF000000)
                        return ResolveFormIdAsync(hexValue, CancellationToken.None).Result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Failed to extract FormID from call stack line: {Line}", line);
        }

        return null;
    }

    /// <summary>
    ///     Checks if a FormID is in a reserved range
    /// </summary>
    private bool IsReservedFormId(uint formId)
    {
        // Reserved ranges for Fallout 4
        return formId >= 0xFF000000 || // Reserved high range
               formId <= 0x00000FFF; // Reserved low range
    }

    /// <summary>
    ///     Checks if a FormID is known to be problematic
    /// </summary>
    private bool IsProblematicFormId(uint formId)
    {
        // Known problematic FormIDs (these would be loaded from configuration)
        var problematicFormIDs = new HashSet<uint>
        {
            0x00000000, // NULL reference
            0xFFFFFFFF // Invalid reference
            // Add more known problematic FormIDs here
        };

        return problematicFormIDs.Contains(formId);
    }

    /// <summary>
    /// Performs FormID matching against crash log plugins with optional database lookups.
    /// </summary>
    /// <param name="formIdMatches">List of extracted FormID matches</param>
    /// <param name="crashLogPlugins">Dictionary of plugins from crash log</param>
    /// <param name="gameName">Name of the game for database queries</param>
    /// <returns>Formatted report of FormID matches</returns>
    public async Task<string> PerformFormIdMatchingAsync(
        List<string> formIdMatches, 
        Dictionary<string, string> crashLogPlugins, 
        string gameName = "Fallout4")
    {
        if (!formIdMatches.Any())
        {
            return "* COULDN'T FIND ANY FORM ID SUSPECTS *";
        }

        var report = new System.Text.StringBuilder();
        var formIdCounts = formIdMatches
            .GroupBy(x => x)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (formIdFull, count) in formIdCounts.OrderBy(kvp => kvp.Key))
        {
            var formIdParts = formIdFull.Split(": ", 2);
            if (formIdParts.Length < 2)
                continue;

            var formIdHex = formIdParts[1];
            var pluginPrefix = formIdHex[..2]; // First 2 characters (plugin index)

            // Find matching plugin
            var matchingPlugin = crashLogPlugins.FirstOrDefault(kvp => 
                kvp.Value.Equals(pluginPrefix, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(matchingPlugin.Key))
            {
                var baseFormId = formIdHex[2..]; // Remove plugin prefix
                
                if (_showFormIdValues && _formIdDbExists)
                {
                    var dbEntry = await LookupFormIdValueAsync(baseFormId, matchingPlugin.Key, gameName);
                    if (!string.IsNullOrEmpty(dbEntry))
                    {
                        report.AppendLine($"- {formIdFull} | [{matchingPlugin.Key}] | {dbEntry} | {count}");
                        continue;
                    }
                }

                report.AppendLine($"- {formIdFull} | [{matchingPlugin.Key}] | {count}");
            }
        }

        report.AppendLine();
        report.AppendLine("[Last number counts how many times each Form ID shows up in the crash log.]");
        report.AppendLine("These Form IDs were caught by Buffout 4 and some of them might be related to this crash.");
        report.AppendLine("You can try searching any listed Form IDs in xEdit and see if they lead to relevant records.");

        return report.ToString();
    }

    /// <summary>
    /// Looks up FormID value in the database with caching.
    /// </summary>
    /// <param name="formId">The FormID (without plugin prefix)</param>
    /// <param name="plugin">The plugin name</param>
    /// <param name="gameName">The game name for database table</param>
    /// <returns>Database entry if found, null otherwise</returns>
    public async Task<string?> LookupFormIdValueAsync(string formId, string plugin, string gameName = "Fallout4")
    {
        if (!_formIdDbExists)
        {
            return null;
        }

        var cacheKey = (formId.ToUpperInvariant(), plugin.ToLowerInvariant());
        
        // Check cache first
        if (_queryCache.TryGetValue(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        // Try each database path
        foreach (var dbPath in _databasePaths)
        {
            if (!File.Exists(dbPath))
                continue;

            try
            {
                using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                await connection.OpenAsync();
                
                var query = $"SELECT entry FROM {gameName} WHERE formid = @formid AND plugin = @plugin COLLATE NOCASE";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@formid", formId);
                command.Parameters.AddWithValue("@plugin", plugin);
                
                var result = await command.ExecuteScalarAsync();
                if (result != null)
                {
                    var entry = result.ToString();
                    if (!string.IsNullOrEmpty(entry))
                    {
                        _queryCache.TryAdd(cacheKey, entry);
                        return entry;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query FormID database at {DbPath}", dbPath);
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts FormIDs from crash log segments, filtering out FF-prefixed FormIDs.
    /// </summary>
    /// <param name="callStackLines">Lines from call stack segments</param>
    /// <returns>List of extracted FormID strings</returns>
    public List<string> ExtractFormIdsFromCallStack(IEnumerable<string> callStackLines)
    {
        var formIdMatches = new List<string>();
        var formIdPattern = new Regex(@"^\s*Form ID:\s*0x([0-9A-F]{8})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (var line in callStackLines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = formIdPattern.Match(line);
            if (match.Success)
            {
                var formIdHex = match.Groups[1].Value.ToUpperInvariant();
                
                // Skip FF-prefixed FormIDs (plugin limit indicators)
                if (!formIdHex.StartsWith("FF"))
                {
                    formIdMatches.Add($"Form ID: {formIdHex}");
                }
            }
        }

        return formIdMatches;
    }

    /// <summary>
    /// Gets database statistics for reporting.
    /// </summary>
    /// <returns>Database status information</returns>
    public FormIdDatabaseStatus GetDatabaseStatus()
    {
        var validDatabases = _databasePaths.Where(File.Exists).ToList();
        
        return new FormIdDatabaseStatus
        {
            DatabaseExists = _formIdDbExists,
            DatabasePaths = validDatabases,
            CacheSize = _queryCache.Count,
            ShowFormIdValues = _showFormIdValues
        };
    }
}

/// <summary>
/// Represents the status of FormID database integration.
/// </summary>
public class FormIdDatabaseStatus
{
    public bool DatabaseExists { get; set; }
    public List<string> DatabasePaths { get; set; } = new();
    public int CacheSize { get; set; }
    public bool ShowFormIdValues { get; set; }
}
