using Classic.Core.Models;
using Classic.ScanLog.Configuration;
using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Analyzers;

/// <summary>
/// Enhanced suspect scanner that uses YAML pattern database from Python CLASSIC
/// </summary>
public class EnhancedSuspectScanner
{
    private readonly ILogger<EnhancedSuspectScanner> _logger;
    private readonly SuspectPatternLoader _patternLoader;
    private readonly SuspectScanner _baseSuspectScanner;
    private SuspectPatternDatabase? _patternDatabase;

    public EnhancedSuspectScanner(
        ILogger<EnhancedSuspectScanner> logger,
        SuspectPatternLoader patternLoader,
        SuspectScanner baseSuspectScanner)
    {
        _logger = logger;
        _patternLoader = patternLoader;
        _baseSuspectScanner = baseSuspectScanner;
    }

    /// <summary>
    /// Initializes the pattern database if not already loaded
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_patternDatabase == null)
        {
            _patternDatabase = await _patternLoader.LoadSuspectPatternsAsync();
            _logger.LogDebug(
                "Initialized suspect scanner with {ErrorPatterns} error patterns and {StackPatterns} stack patterns",
                _patternDatabase.CrashlogErrorCheck.Count,
                _patternDatabase.CrashlogStackCheck.Count);
        }
    }

    /// <summary>
    /// Scans a crash log for all known suspects using both built-in and YAML patterns
    /// </summary>
    public async Task<List<DetectedSuspect>> ScanForSuspectsAsync(
        CrashLog crashLog,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting enhanced suspect scan for: {FileName}", crashLog.FileName);

            await InitializeAsync();

            var allSuspects = new List<DetectedSuspect>();

            // Get built-in suspects from base scanner
            var builtInSuspects = await _baseSuspectScanner.ScanForSuspectsAsync(crashLog, cancellationToken);
            allSuspects.AddRange(builtInSuspects);

            if (_patternDatabase != null)
            {
                // Scan for YAML-based suspects
                var yamlSuspects = await ScanYamlPatternsAsync(crashLog, cancellationToken);
                allSuspects.AddRange(yamlSuspects);

                // Check for DLL crashes using enhanced logic
                var dllSuspect = CheckDllCrash(crashLog.MainError);
                if (dllSuspect != null) allSuspects.Add(dllSuspect);
            }

            // Remove duplicates based on name and sort by severity
            var uniqueSuspects = allSuspects
                .GroupBy(s => s.Name)
                .Select(g => g.OrderByDescending(s => s.Severity).First())
                .OrderByDescending(s => s.Severity)
                .ThenByDescending(s => s.Confidence)
                .ToList();

            _logger.LogDebug("Found {Count} unique suspects in: {FileName}", uniqueSuspects.Count, crashLog.FileName);
            return uniqueSuspects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan for suspects in: {FileName}", crashLog.FileName);
            throw;
        }
    }

    /// <summary>
    /// Scans using YAML-based patterns
    /// </summary>
    private async Task<List<DetectedSuspect>> ScanYamlPatternsAsync(
        CrashLog crashLog,
        CancellationToken cancellationToken)
    {
        if (_patternDatabase == null)
            return new List<DetectedSuspect>();

        return await Task.Run(() =>
        {
            var suspects = new List<DetectedSuspect>();

            // Scan simple error patterns
            var errorSuspects = ScanMainErrorPatterns(crashLog);
            suspects.AddRange(errorSuspects);

            // Scan complex stack patterns  
            var stackSuspects = ScanStackPatterns(crashLog);
            suspects.AddRange(stackSuspects);

            return suspects;
        }, cancellationToken);
    }

    /// <summary>
    /// Scans main error patterns from YAML database
    /// </summary>
    private List<DetectedSuspect> ScanMainErrorPatterns(CrashLog crashLog)
    {
        if (_patternDatabase?.CrashlogErrorCheck == null)
            return new List<DetectedSuspect>();

        var suspects = new List<DetectedSuspect>();
        var mainError = crashLog.MainError ?? string.Empty;

        foreach (var (errorKey, signal) in _patternDatabase.CrashlogErrorCheck)
        {
            if (string.IsNullOrEmpty(signal) || !mainError.Contains(signal, StringComparison.OrdinalIgnoreCase))
                continue;

            // Parse error information (format: "severity | name")
            var parts = errorKey.Split(" | ", 2);
            if (parts.Length != 2 || !int.TryParse(parts[0], out var severity))
                continue;

            var errorName = parts[1];

            suspects.Add(new DetectedSuspect
            {
                Name = errorName,
                Description = $"Main error pattern match: {signal}",
                Severity = severity,
                MatchedPatterns = new List<string> { signal },
                Solutions = GenerateSolutions(errorName, severity),
                Confidence = 0.9, // High confidence for main error matches
                DocumentationUrl = GetDocumentationUrl(errorName)
            });

            _logger.LogDebug("Found main error suspect: {Name} (Severity: {Severity})", errorName, severity);
        }

        return suspects;
    }

    /// <summary>
    /// Scans complex stack patterns from YAML database
    /// </summary>
    private List<DetectedSuspect> ScanStackPatterns(CrashLog crashLog)
    {
        if (_patternDatabase?.CrashlogStackCheck == null)
            return new List<DetectedSuspect>();

        var suspects = new List<DetectedSuspect>();
        var mainError = crashLog.MainError ?? string.Empty;
        var callStackContent = GetCallStackContent(crashLog);

        foreach (var (errorKey, signalList) in _patternDatabase.CrashlogStackCheck)
        {
            if (signalList == null || signalList.Count == 0)
                continue;

            // Parse error information (format: "severity | name")
            var parts = errorKey.Split(" | ", 2);
            if (parts.Length != 2 || !int.TryParse(parts[0], out var severity))
                continue;

            var errorName = parts[1];

            // Initialize match status tracking
            var matchStatus = new AdvancedMatchStatus();
            var matchedPatterns = new List<string>();

            // Process each signal in the list
            var shouldSkipError = false;
            foreach (var signal in signalList)
            {
                if (string.IsNullOrEmpty(signal))
                    continue;

                if (ProcessStackSignal(signal, mainError, callStackContent, matchStatus, matchedPatterns))
                {
                    shouldSkipError = true;
                    break; // NOT condition met, skip this suspect
                }
            }

            if (shouldSkipError)
                continue;

            // Determine if we have a match based on processed signals
            if (IsStackSuspectMatch(matchStatus))
            {
                suspects.Add(new DetectedSuspect
                {
                    Name = errorName,
                    Description = $"Stack pattern match detected: {errorName}",
                    Severity = severity,
                    MatchedPatterns = matchedPatterns,
                    Solutions = GenerateSolutions(errorName, severity),
                    Confidence = CalculateStackConfidence(matchStatus, matchedPatterns.Count, signalList.Count),
                    DocumentationUrl = GetDocumentationUrl(errorName)
                });

                _logger.LogDebug("Found stack suspect: {Name} (Severity: {Severity}, Patterns: {Count})",
                    errorName, severity, matchedPatterns.Count);
            }
        }

        return suspects;
    }

    /// <summary>
    /// Processes a stack signal pattern
    /// </summary>
    private bool ProcessStackSignal(
        string signal,
        string mainError,
        string callStackContent,
        AdvancedMatchStatus matchStatus,
        List<string> matchedPatterns)
    {
        // Constants for signal modifiers
        const string MainErrorRequired = "ME-REQ";
        const string MainErrorOptional = "ME-OPT";
        const string CallStackNegative = "NOT";

        if (!signal.Contains('|'))
        {
            // Simple case: direct string match in callstack
            if (callStackContent.Contains(signal, StringComparison.OrdinalIgnoreCase))
            {
                matchStatus.StackFound = true;
                matchedPatterns.Add(signal);
            }

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
                {
                    matchStatus.ErrorReqFound = true;
                    matchedPatterns.Add($"{MainErrorRequired}|{signalString}");
                }

                break;

            case MainErrorOptional:
                if (mainError.Contains(signalString, StringComparison.OrdinalIgnoreCase))
                {
                    matchStatus.ErrorOptFound = true;
                    matchedPatterns.Add($"{MainErrorOptional}|{signalString}");
                }

                break;

            case CallStackNegative:
                // Return true to break out of loop if NOT condition is met
                if (callStackContent.Contains(signalString, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("NOT condition met for {Signal}, skipping suspect", signalString);
                    return true;
                }

                break;

            default:
                // Check for numeric occurrence counting (e.g., "3|pattern" for minimum 3 occurrences)
                if (int.TryParse(signalModifier, out var minOccurrences))
                {
                    var occurrences = CountOccurrences(callStackContent, signalString);
                    if (occurrences >= minOccurrences)
                    {
                        matchStatus.StackFound = true;
                        matchedPatterns.Add($"{minOccurrences}|{signalString}");
                    }
                }
                else if (callStackContent.Contains(signalString, StringComparison.OrdinalIgnoreCase))
                {
                    // Fallback: treat as regular pattern
                    matchStatus.StackFound = true;
                    matchedPatterns.Add(signalString);
                }

                break;
        }

        return false;
    }

    /// <summary>
    /// Determines if stack patterns constitute a suspect match
    /// </summary>
    private bool IsStackSuspectMatch(AdvancedMatchStatus matchStatus)
    {
        if (matchStatus.HasRequiredItem) return matchStatus.ErrorReqFound;
        return matchStatus.ErrorOptFound || matchStatus.StackFound;
    }

    /// <summary>
    /// Gets call stack content as a single string for pattern matching
    /// </summary>
    private string GetCallStackContent(CrashLog crashLog)
    {
        var callStackSegments = new[] { "CallStack", "Modules", "SystemSpecs" };
        var content = new System.Text.StringBuilder();

        foreach (var segmentName in callStackSegments)
            if (crashLog.Segments.TryGetValue(segmentName, out var lines))
                foreach (var line in lines)
                    content.AppendLine(line);

        return content.ToString();
    }

    /// <summary>
    /// Counts occurrences of a pattern in text
    /// </summary>
    private int CountOccurrences(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return 0;

        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    /// <summary>
    /// Calculates confidence for stack pattern matches
    /// </summary>
    private double CalculateStackConfidence(AdvancedMatchStatus matchStatus, int matchedCount, int totalCount)
    {
        var confidence = 0.4; // Base confidence for stack matches

        if (matchStatus.ErrorReqFound || matchStatus.ErrorOptFound)
            confidence += 0.4; // Higher confidence for main error matches

        if (matchStatus.StackFound)
            confidence += 0.2; // Additional confidence for stack matches

        // Adjust based on pattern match ratio
        if (totalCount > 0)
        {
            var matchRatio = (double)matchedCount / totalCount;
            confidence += matchRatio * 0.2;
        }

        return Math.Min(confidence, 1.0);
    }

    /// <summary>
    /// Generates solutions based on suspect name and severity
    /// </summary>
    private List<string> GenerateSolutions(string suspectName, int severity)
    {
        var solutions = new List<string>();

        // Generate solutions based on suspect type
        if (suspectName.ToLower().Contains("crash"))
            solutions.Add($"Investigate {suspectName.Replace(" Crash", "")} related issues");

        // Add severity-based solutions
        switch (severity)
        {
            case 6:
            case 5:
                solutions.Add("This is a critical issue that requires immediate attention");
                solutions.Add("Backup your save files before making changes");
                break;
            case 4:
                solutions.Add("This is a significant issue that may cause frequent crashes");
                break;
            case 3:
                solutions.Add("This is a moderate issue that may cause occasional problems");
                break;
            case 2:
            case 1:
                solutions.Add("This is a minor issue with low impact");
                break;
        }

        // Add type-specific solutions
        if (suspectName.ToLower().Contains("mod"))
        {
            solutions.Add("Check for mod conflicts or outdated versions");
            solutions.Add("Review mod load order");
        }

        if (suspectName.ToLower().Contains("script"))
        {
            solutions.Add("Check Papyrus logs for script errors");
            solutions.Add("Increase script memory limits in INI files");
        }

        if (suspectName.ToLower().Contains("texture") || suspectName.ToLower().Contains("graphics"))
        {
            solutions.Add("Update graphics drivers");
            solutions.Add("Check for corrupted texture files");
        }

        return solutions.Any() ? solutions : new List<string> { "No specific solutions available for this issue" };
    }

    /// <summary>
    /// Gets documentation URL for a suspect type
    /// </summary>
    private string? GetDocumentationUrl(string suspectName)
    {
        // Return generic CLASSIC documentation for now
        // This could be enhanced to provide specific URLs based on suspect type
        return "https://docs.google.com/document/d/17FzeIMJ256xE85XdjoPvv_Zi3C5uHeSTQh6wOZugs4c";
    }

    /// <summary>
    /// Enhanced DLL crash detection
    /// </summary>
    private DetectedSuspect? CheckDllCrash(string? mainError)
    {
        return _baseSuspectScanner.CheckDllCrash(mainError ?? string.Empty);
    }
}
