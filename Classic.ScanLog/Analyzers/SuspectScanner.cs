using System.Text.RegularExpressions;
using Classic.Core.Models;
using Classic.ScanLog.Models;
using Classic.ScanLog.Parsers;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Analyzers;

/// <summary>
///     Scans crash logs for known crash suspects and patterns.
/// </summary>
public class SuspectScanner
{
    private readonly ScanLogConfiguration _configuration;
    private readonly ILogger<SuspectScanner> _logger;

    public SuspectScanner(
        ILogger<SuspectScanner> logger,
        ScanLogConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    ///     Scans a crash log for known suspects asynchronously
    /// </summary>
    public async Task<List<DetectedSuspect>> ScanForSuspectsAsync(
        CrashLog crashLog,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting suspect scan for: {FileName}", crashLog.FileName);

            var detectedSuspects = new List<DetectedSuspect>();

            // Scan all configured suspects
            foreach (var suspect in _configuration.CrashSuspects.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var detection = await ScanForSpecificSuspectAsync(crashLog, suspect, cancellationToken);
                if (detection != null) detectedSuspects.Add(detection);
            }

            // Scan for built-in patterns
            var builtInSuspects = await ScanForBuiltInSuspectsAsync(crashLog, cancellationToken);
            detectedSuspects.AddRange(builtInSuspects);

            // Sort by severity (highest first)
            detectedSuspects.Sort((a, b) => b.Severity.CompareTo(a.Severity));

            _logger.LogDebug("Found {Count} suspects in: {FileName}", detectedSuspects.Count, crashLog.FileName);
            return detectedSuspects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan for suspects in: {FileName}", crashLog.FileName);
            throw;
        }
    }

    /// <summary>
    ///     Scans for a specific configured suspect
    /// </summary>
    private async Task<DetectedSuspect?> ScanForSpecificSuspectAsync(
        CrashLog crashLog,
        CrashSuspect suspect,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var matchedPatterns = new List<string>();
            var confidence = 0.0;

            // Check patterns against all content
            foreach (var pattern in suspect.Patterns)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    var allContent = string.Join("\n", crashLog.RawContent);

                    if (regex.IsMatch(allContent))
                    {
                        matchedPatterns.Add(pattern);
                        confidence += 1.0 / suspect.Patterns.Count; // Equal weight for each pattern
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid regex pattern for suspect {SuspectName}: {Pattern}",
                        suspect.Name, pattern);
                }
            }

            if (matchedPatterns.Count > 0)
                return new DetectedSuspect
                {
                    Name = suspect.Name,
                    Description = suspect.Description,
                    Severity = suspect.Severity,
                    MatchedPatterns = matchedPatterns,
                    Solutions = suspect.Solutions.ToList(),
                    DocumentationUrl = suspect.DocumentationUrl,
                    Confidence = confidence
                };

            return null;
        }, cancellationToken);
    }

    /// <summary>
    ///     Scans for built-in crash suspects
    /// </summary>
    private async Task<List<DetectedSuspect>> ScanForBuiltInSuspectsAsync(
        CrashLog crashLog,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var suspects = new List<DetectedSuspect>();

            // BA2 Limit Crash
            var ba2LimitSuspect = CheckBa2LimitCrash(crashLog);
            if (ba2LimitSuspect != null) suspects.Add(ba2LimitSuspect);

            // Memory Access Violation
            var memoryAccessSuspect = CheckMemoryAccessViolation(crashLog);
            if (memoryAccessSuspect != null) suspects.Add(memoryAccessSuspect);

            // Plugin Limit Crash
            var pluginLimitSuspect = CheckPluginLimitCrash(crashLog);
            if (pluginLimitSuspect != null) suspects.Add(pluginLimitSuspect);

            // Navmesh Issues
            var navmeshSuspect = CheckNavmeshIssues(crashLog);
            if (navmeshSuspect != null) suspects.Add(navmeshSuspect);

            // Script Related Crashes
            var scriptSuspect = CheckScriptRelatedCrashes(crashLog);
            if (scriptSuspect != null) suspects.Add(scriptSuspect);

            // Graphics/Texture Issues
            var graphicsSuspect = CheckGraphicsIssues(crashLog);
            if (graphicsSuspect != null) suspects.Add(graphicsSuspect);

            // F4SE Plugin Issues
            var f4SeSuspect = CheckF4SePluginIssues(crashLog);
            if (f4SeSuspect != null) suspects.Add(f4SeSuspect);

            return suspects;
        }, cancellationToken);
    }

    /// <summary>
    ///     Checks for BA2 limit related crashes
    /// </summary>
    private DetectedSuspect? CheckBa2LimitCrash(CrashLog crashLog)
    {
        var patterns = new[]
        {
            @"BSResource::LooseFileLocation",
            @"BSResource::ArchiveCache",
            @"\.ba2",
            @"EXCEPTION_ACCESS_VIOLATION.*Archive"
        };

        var matchedPatterns = new List<string>();
        var allContent = string.Join(" ", crashLog.RawContent);

        foreach (var pattern in patterns)
            if (Regex.IsMatch(allContent, pattern, RegexOptions.IgnoreCase))
                matchedPatterns.Add(pattern);

        if (matchedPatterns.Count >= 2) // Need at least 2 patterns to match
            return new DetectedSuspect
            {
                Name = "BA2 Limit Crash",
                Description = "Too many BA2 archives or corrupted archive files causing memory access issues",
                Severity = 6,
                MatchedPatterns = matchedPatterns,
                Solutions = new List<string>
                {
                    "Reduce the number of BA2 archives",
                    "Check for corrupted mod archives",
                    "Use Archive2 to rebuild problematic BA2 files",
                    "Consider using loose files instead of BA2 for some mods"
                },
                DocumentationUrl = "https://docs.google.com/document/d/17FzeIMJ256xE85XdjoPvv_Zi3C5uHeSTQh6wOZugs4c",
                Confidence = (double)matchedPatterns.Count / patterns.Length
            };

        return null;
    }

    /// <summary>
    ///     Checks for memory access violation issues
    /// </summary>
    private DetectedSuspect? CheckMemoryAccessViolation(CrashLog crashLog)
    {
        if (!crashLog.MainError.Contains("EXCEPTION_ACCESS_VIOLATION"))
            return null;

        var severity = 7;
        var solutions = new List<string>
        {
            "Check for mod conflicts",
            "Verify all mods are compatible with your game version",
            "Run memory diagnostic tools",
            "Check for corrupted game files"
        };

        // Check for NULL pointer access (more severe)
        if (crashLog.RawContent.Any(line =>
                line.Contains("0x000000000000") || line.Contains("Tried to read memory at 0x000000000000")))
        {
            severity = 9;
            solutions.Insert(0, "NULL pointer access detected - likely a serious mod conflict or corruption");
        }

        return new DetectedSuspect
        {
            Name = "Memory Access Violation",
            Description = "The game attempted to access invalid memory locations",
            Severity = severity,
            MatchedPatterns = new List<string> { "EXCEPTION_ACCESS_VIOLATION" },
            Solutions = solutions,
            Confidence = 1.0
        };
    }

    /// <summary>
    ///     Checks for plugin limit related crashes
    /// </summary>
    private DetectedSuspect? CheckPluginLimitCrash(CrashLog crashLog)
    {
        if (!crashLog.HasValidPluginList())
            return null;

        var (light, regular, total) = crashLog.GetPluginCounts();

        if (total > 255)
            return new DetectedSuspect
            {
                Name = "Plugin Limit Exceeded",
                Description = $"Too many plugins loaded ({total}/255). This exceeds the game engine limit.",
                Severity = 8,
                MatchedPatterns = new List<string> { $"Total plugins: {total}" },
                Solutions = new List<string>
                {
                    "Reduce the number of plugins below 255",
                    "Merge compatible plugins using FO4Edit",
                    "Convert ESP files to ESL (light) format where possible",
                    "Remove unnecessary plugins"
                },
                Confidence = 1.0
            };

        if (total > 200)
            return new DetectedSuspect
            {
                Name = "High Plugin Count",
                Description = $"High number of plugins ({total}). This may cause stability issues.",
                Severity = 4,
                MatchedPatterns = new List<string> { $"Total plugins: {total}" },
                Solutions = new List<string>
                {
                    "Consider reducing plugin count for better stability",
                    "Monitor for mod conflicts",
                    "Use a mod manager to organize load order"
                },
                Confidence = 1.0
            };

        return null;
    }

    /// <summary>
    ///     Checks for navmesh related issues
    /// </summary>
    private DetectedSuspect? CheckNavmeshIssues(CrashLog crashLog)
    {
        var navmeshPatterns = new[]
        {
            @"BSNavmesh",
            @"kNAVM",
            @"PathManager",
            @"FindPortalTriangles",
            @"NavMesh\*"
        };

        var matchedPatterns = new List<string>();
        var allContent = string.Join(" ", crashLog.RawContent);

        foreach (var pattern in navmeshPatterns)
            if (Regex.IsMatch(allContent, pattern, RegexOptions.IgnoreCase))
                matchedPatterns.Add(pattern);

        if (matchedPatterns.Count > 0)
            return new DetectedSuspect
            {
                Name = "Navmesh Issues",
                Description = "Problems with navigation mesh causing pathfinding crashes",
                Severity = 5,
                MatchedPatterns = matchedPatterns,
                Solutions = new List<string>
                {
                    "Check for mods that edit navmesh data",
                    "Look for navmesh conflicts between mods",
                    "Regenerate navmesh if you've added new areas",
                    "Disable mods that add NPCs or change AI behavior temporarily"
                },
                Confidence = (double)matchedPatterns.Count / navmeshPatterns.Length
            };

        return null;
    }

    /// <summary>
    ///     Checks for script related crashes
    /// </summary>
    private DetectedSuspect? CheckScriptRelatedCrashes(CrashLog crashLog)
    {
        var scriptPatterns = new[]
        {
            @"Papyrus",
            @"Script",
            @"\.psc",
            @"ScriptObject",
            @"VMStackFrame"
        };

        var matchedPatterns = new List<string>();
        var allContent = string.Join(" ", crashLog.RawContent);

        foreach (var pattern in scriptPatterns)
            if (Regex.IsMatch(allContent, pattern, RegexOptions.IgnoreCase))
                matchedPatterns.Add(pattern);

        if (matchedPatterns.Count > 0)
            return new DetectedSuspect
            {
                Name = "Script Related Issues",
                Description = "Problems with Papyrus scripts or script execution",
                Severity = 4,
                MatchedPatterns = matchedPatterns,
                Solutions = new List<string>
                {
                    "Check for script-heavy mods causing performance issues",
                    "Increase Papyrus memory settings in INI files",
                    "Look for script errors in Papyrus logs",
                    "Disable scripted mods temporarily to test"
                },
                Confidence = (double)matchedPatterns.Count / scriptPatterns.Length
            };

        return null;
    }

    /// <summary>
    ///     Checks for graphics/texture related issues
    /// </summary>
    private DetectedSuspect? CheckGraphicsIssues(CrashLog crashLog)
    {
        var graphicsPatterns = new[]
        {
            @"D3D11",
            @"dxgi",
            @"CreateTexture2D",
            @"Graphics",
            @"\.dds",
            @"BSTextureStreamer"
        };

        var matchedPatterns = new List<string>();
        var allContent = string.Join(" ", crashLog.RawContent);

        foreach (var pattern in graphicsPatterns)
            if (Regex.IsMatch(allContent, pattern, RegexOptions.IgnoreCase))
                matchedPatterns.Add(pattern);

        if (matchedPatterns.Count >= 2)
            return new DetectedSuspect
            {
                Name = "Graphics/Texture Issues",
                Description = "Problems with graphics rendering or texture loading",
                Severity = 3,
                MatchedPatterns = matchedPatterns,
                Solutions = new List<string>
                {
                    "Update graphics drivers",
                    "Check for corrupted texture files",
                    "Reduce texture quality settings",
                    "Verify video memory is not overallocated"
                },
                Confidence = (double)matchedPatterns.Count / graphicsPatterns.Length
            };

        return null;
    }

    /// <summary>
    ///     Checks for F4SE plugin related issues
    /// </summary>
    private DetectedSuspect? CheckF4SePluginIssues(CrashLog crashLog)
    {
        if (!crashLog.Segments.ContainsKey("F4SEPlugins"))
            return null;

        var f4SePlugins = crashLog.Segments["F4SEPlugins"];
        var problematicPlugins = new List<string>();

        // Check for known problematic F4SE plugins
        var knownProblematic = new[]
        {
            "achievement",
            "oldversion",
            "outdated"
        };

        foreach (var line in f4SePlugins)
        foreach (var problematic in knownProblematic)
            if (line.ToLower().Contains(problematic))
                problematicPlugins.Add(line.Trim());

        if (problematicPlugins.Count > 0)
            return new DetectedSuspect
            {
                Name = "F4SE Plugin Issues",
                Description = "Potentially problematic F4SE plugins detected",
                Severity = 3,
                MatchedPatterns = problematicPlugins,
                Solutions = new List<string>
                {
                    "Update F4SE plugins to latest versions",
                    "Remove outdated or incompatible plugins",
                    "Check F4SE compatibility with game version"
                },
                Confidence = 1.0
            };

        return null;
    }

    /// <summary>
    /// Advanced suspect scanning with ME-REQ, ME-OPT, NOT, and occurrence counting patterns.
    /// </summary>
    /// <param name="crashLog">The crash log to analyze</param>
    /// <param name="advancedSuspects">Dictionary of advanced suspect patterns</param>
    /// <returns>List of detected suspects using advanced patterns</returns>
    public List<DetectedSuspect> ScanWithAdvancedPatterns(CrashLog crashLog,
        Dictionary<string, List<string>> advancedSuspects)
    {
        var detectedSuspects = new List<DetectedSuspect>();
        var mainError = crashLog.MainError ?? string.Empty;
        var callStackIntact = GetCallStackAsString(crashLog);

        foreach (var (errorKey, signalList) in advancedSuspects)
        {
            // Parse error information (format: "severity | name")
            var parts = errorKey.Split(" | ", 2);
            if (parts.Length != 2 || !int.TryParse(parts[0], out var severity))
                continue;

            var errorName = parts[1];

            // Initialize match status tracking
            var matchStatus = new AdvancedMatchStatus();

            // Process each signal in the list
            var shouldSkipError = false;
            foreach (var signal in signalList)
                if (ProcessAdvancedSignal(signal, mainError, callStackIntact, matchStatus))
                {
                    shouldSkipError = true;
                    break; // NOT condition met, skip this suspect
                }

            if (shouldSkipError)
                continue;

            // Determine if we have a match based on processed signals
            if (IsAdvancedSuspectMatch(matchStatus))
                detectedSuspects.Add(new DetectedSuspect
                {
                    Name = errorName,
                    Description = $"Advanced pattern match detected: {errorName}",
                    Severity = severity,
                    MatchedPatterns = GetMatchedPatterns(signalList, mainError, callStackIntact),
                    Confidence = CalculateConfidence(matchStatus),
                    Solutions = new List<string> { $"Investigate {errorName} related issues" }
                });
        }

        return detectedSuspects;
    }

    /// <summary>
    /// Processes an individual advanced signal pattern.
    /// </summary>
    /// <param name="signal">The signal to process</param>
    /// <param name="mainError">Main error message</param>
    /// <param name="callStackIntact">Complete call stack as string</param>
    /// <param name="matchStatus">Match status to update</param>
    /// <returns>True if processing should stop (NOT condition met)</returns>
    private bool ProcessAdvancedSignal(string signal, string mainError, string callStackIntact,
        AdvancedMatchStatus matchStatus)
    {
        // Constants for signal modifiers
        const string MainErrorRequired = "ME-REQ";
        const string MainErrorOptional = "ME-OPT";
        const string CallStackNegative = "NOT";

        if (!signal.Contains('|'))
        {
            // Simple case: direct string match in callstack
            if (callStackIntact.Contains(signal, StringComparison.OrdinalIgnoreCase)) matchStatus.StackFound = true;
            return false;
        }

        var parts = signal.Split('|', 2);
        if (parts.Length != 2)
            return false;

        var signalModifier = parts[0].Trim();
        var signalString = parts[1].Trim();

        // Process based on signal modifier
        switch (signalModifier)
        {
            case MainErrorRequired:
                matchStatus.HasRequiredItem = true;
                if (mainError.Contains(signalString, StringComparison.OrdinalIgnoreCase))
                    matchStatus.ErrorReqFound = true;
                break;

            case MainErrorOptional:
                if (mainError.Contains(signalString, StringComparison.OrdinalIgnoreCase))
                    matchStatus.ErrorOptFound = true;
                break;

            case CallStackNegative:
                // Return true to break out of loop if NOT condition is met
                return callStackIntact.Contains(signalString, StringComparison.OrdinalIgnoreCase);

            default:
                // Check for numeric occurrence counting (e.g., "3|pattern" for minimum 3 occurrences)
                if (int.TryParse(signalModifier, out var minOccurrences))
                {
                    var regex = new Regex(Regex.Escape(signalString), RegexOptions.IgnoreCase);
                    var matches = regex.Matches(callStackIntact);
                    if (matches.Count >= minOccurrences) matchStatus.StackFound = true;
                }

                break;
        }

        return false;
    }

    /// <summary>
    /// Determines if current error conditions constitute a suspect match.
    /// </summary>
    /// <param name="matchStatus">The match status to evaluate</param>
    /// <returns>True if conditions indicate a suspect match</returns>
    private bool IsAdvancedSuspectMatch(AdvancedMatchStatus matchStatus)
    {
        if (matchStatus.HasRequiredItem) return matchStatus.ErrorReqFound;
        return matchStatus.ErrorOptFound || matchStatus.StackFound;
    }

    /// <summary>
    /// Gets the complete call stack as a single string for pattern matching.
    /// </summary>
    /// <param name="crashLog">The crash log</param>
    /// <returns>Call stack content as string</returns>
    private string GetCallStackAsString(CrashLog crashLog)
    {
        var callStackSegments = new[] { "PROBABLE CALL STACK", "STACK", "MODULES" };
        var callStackBuilder = new System.Text.StringBuilder();

        foreach (var segmentName in callStackSegments)
            if (crashLog.Segments.TryGetValue(segmentName, out var lines))
                foreach (var line in lines)
                    callStackBuilder.AppendLine(line);

        return callStackBuilder.ToString();
    }

    /// <summary>
    /// Gets the patterns that actually matched for reporting.
    /// </summary>
    /// <param name="signalList">List of signals to check</param>
    /// <param name="mainError">Main error message</param>
    /// <param name="callStackIntact">Call stack content</param>
    /// <returns>List of matched patterns</returns>
    private List<string> GetMatchedPatterns(List<string> signalList, string mainError, string callStackIntact)
    {
        var matchedPatterns = new List<string>();

        foreach (var signal in signalList)
            if (!signal.Contains('|'))
            {
                if (callStackIntact.Contains(signal, StringComparison.OrdinalIgnoreCase)) matchedPatterns.Add(signal);
            }
            else
            {
                var parts = signal.Split('|', 2);
                if (parts.Length == 2)
                {
                    var signalString = parts[1].Trim();
                    var modifier = parts[0].Trim();

                    if ((modifier == "ME-REQ" || modifier == "ME-OPT") &&
                        mainError.Contains(signalString, StringComparison.OrdinalIgnoreCase))
                        matchedPatterns.Add($"{modifier}|{signalString}");
                    else if (callStackIntact.Contains(signalString, StringComparison.OrdinalIgnoreCase))
                        matchedPatterns.Add(signalString);
                }
            }

        return matchedPatterns;
    }

    /// <summary>
    /// Calculates confidence based on match status.
    /// </summary>
    /// <param name="matchStatus">The match status</param>
    /// <returns>Confidence score between 0 and 1</returns>
    private double CalculateConfidence(AdvancedMatchStatus matchStatus)
    {
        var confidence = 0.5; // Base confidence

        if (matchStatus.ErrorReqFound || matchStatus.ErrorOptFound)
            confidence += 0.3; // Higher confidence for main error matches

        if (matchStatus.StackFound)
            confidence += 0.2; // Additional confidence for stack matches

        return Math.Min(confidence, 1.0);
    }

    /// <summary>
    /// Checks for DLL-related crashes in the main error message.
    /// </summary>
    /// <param name="mainError">The main error message</param>
    /// <returns>Detected suspect if DLL crash found, null otherwise</returns>
    public DetectedSuspect? CheckDllCrash(string mainError)
    {
        if (string.IsNullOrEmpty(mainError))
            return null;

        var mainErrorLower = mainError.ToLowerInvariant();

        if (mainErrorLower.Contains(".dll") && !mainErrorLower.Contains("tbbmalloc"))
            return new DetectedSuspect
            {
                Name = "DLL Crash Detected",
                Description = "Main error reports that a DLL file was involved in this crash",
                Severity = 4, // High severity
                MatchedPatterns = new List<string> { ".dll" },
                Solutions = new List<string>
                {
                    "If the DLL belongs to a mod, that mod is a prime suspect for the crash",
                    "Check if the DLL is up to date",
                    "Try disabling the mod associated with the DLL"
                },
                Confidence = 0.8
            };

        return null;
    }
}

/// <summary>
/// Tracks the status of advanced pattern matching.
/// </summary>
public class AdvancedMatchStatus
{
    public bool HasRequiredItem { get; set; }
    public bool ErrorReqFound { get; set; }
    public bool ErrorOptFound { get; set; }
    public bool StackFound { get; set; }
}
