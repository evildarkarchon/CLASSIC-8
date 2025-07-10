using System.Globalization;
using System.Text.RegularExpressions;
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

    public FormIdAnalyzer(
        ILogger<FormIdAnalyzer> logger,
        ScanLogConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
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
                if (formIdString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    formId.FormIdValue = Convert.ToUInt32(formIdString, 16);
                else
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
               Regex.IsMatch(line, @"0x[0-9A-F]{8}") ||
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
            var hexMatches = Regex.Matches(line, @"0x([0-9A-F]{8})");
            foreach (Match match in hexMatches)
            {
                var hexValue = match.Groups[1].Value;
                if (uint.TryParse(hexValue, NumberStyles.HexNumber, null, out var value))
                    // Simple heuristic: if it looks like a FormID (not too high, not zero)
                    if (value > 0 && value < 0xFF000000)
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
               (formId >= 0x00000000 && formId <= 0x00000FFF); // Reserved low range
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
}
