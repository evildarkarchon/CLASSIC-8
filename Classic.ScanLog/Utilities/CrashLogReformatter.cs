using System.Text;
using Microsoft.Extensions.Logging;

namespace Classic.ScanLog.Utilities;

/// <summary>
/// Reformats crash log files to normalize their format and structure.
/// Implements functionality equivalent to the Python crashlogs_reformat function.
/// </summary>
public class CrashLogReformatter
{
    private readonly ILogger<CrashLogReformatter> _logger;

    public CrashLogReformatter(ILogger<CrashLogReformatter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reformats a batch of crash log files asynchronously
    /// </summary>
    /// <param name="crashLogPaths">List of crash log file paths to reformat</param>
    /// <param name="removePatterns">Patterns to remove when simplifying logs</param>
    /// <param name="simplifyLogs">Whether to remove lines matching remove patterns</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ReformatCrashLogsAsync(
        IEnumerable<string> crashLogPaths,
        IEnumerable<string> removePatterns,
        bool simplifyLogs = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initiated crash log file reformat");

        var removeList = removePatterns.ToList();
        var tasks = crashLogPaths.Select(path =>
            ReformatSingleLogAsync(path, removeList, simplifyLogs, cancellationToken));

        await Task.WhenAll(tasks);

        _logger.LogDebug("Completed crash log file reformat");
    }

    /// <summary>
    /// Reformats a single crash log file asynchronously
    /// </summary>
    /// <param name="filePath">Path to the crash log file</param>
    /// <param name="removePatterns">Patterns to remove when simplifying</param>
    /// <param name="simplifyLogs">Whether to remove lines matching patterns</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ReformatSingleLogAsync(
        string filePath,
        IList<string> removePatterns,
        bool simplifyLogs = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Reformatting crash log: {FilePath}", filePath);

            // Read all lines from the file
            var originalLines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);

            // Process lines from bottom to top to handle PLUGINS section correctly
            var processedLines = ProcessLinesInReverse(originalLines, removePatterns, simplifyLogs);

            // Write reformatted content back to file
            await File.WriteAllLinesAsync(filePath, processedLines, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug("Successfully reformatted crash log: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reformat crash log: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Processes crash log lines in reverse order to handle PLUGINS section formatting
    /// </summary>
    private static List<string> ProcessLinesInReverse(
        string[] originalLines,
        IList<string> removePatterns,
        bool simplifyLogs)
    {
        var processedLinesReversed = new List<string>(originalLines.Length);
        var inPluginsSection = true; // Start true since we're processing from bottom up

        // Iterate over lines from bottom to top to correctly handle PLUGINS section logic
        for (var i = originalLines.Length - 1; i >= 0; i--)
        {
            var line = originalLines[i];

            // Track if we're in the PLUGINS section (from bottom up)
            if (inPluginsSection && line.StartsWith("PLUGINS:"))
                inPluginsSection = false; // Exited the PLUGINS section (from bottom)

            // Skip lines if simplify logs is enabled and line contains remove patterns
            if (simplifyLogs && removePatterns.Any(pattern => line.Contains(pattern))) continue; // Skip this line

            // Reformat lines within the PLUGINS section
            if (inPluginsSection && line.Contains('['))
            {
                var reformattedLine = ReformatPluginLine(line);
                processedLinesReversed.Add(reformattedLine);
            }
            else
            {
                // Line is not removed or modified, keep as is
                processedLinesReversed.Add(line);
            }
        }

        // Reverse the list back to original order
        processedLinesReversed.Reverse();
        return processedLinesReversed;
    }

    /// <summary>
    /// Reformats a plugin line to normalize load order format
    /// Replaces spaces inside [brackets] with 0s for consistency
    /// </summary>
    /// <param name="line">Original plugin line</param>
    /// <returns>Reformatted line</returns>
    private static string ReformatPluginLine(string line)
    {
        try
        {
            // Find the bracket section in the line
            var openBracketIndex = line.IndexOf('[');
            var closeBracketIndex = line.IndexOf(']', openBracketIndex + 1);

            if (openBracketIndex == -1 ||
                closeBracketIndex == -1) return line; // No valid brackets found, return original

            // Split the line into parts
            var indent = line[..openBracketIndex];
            var bracketContent = line[(openBracketIndex + 1)..closeBracketIndex];
            var remainder = line[(closeBracketIndex + 1)..];

            // Replace spaces with 0s in the bracket content
            var normalizedBracketContent = bracketContent.Replace(' ', '0');

            // Reconstruct the line
            return $"{indent}[{normalizedBracketContent}]{remainder}";
        }
        catch
        {
            // If anything goes wrong with parsing, return the original line
            return line;
        }
    }

    /// <summary>
    /// Reformats crash log content in memory without writing to file
    /// </summary>
    /// <param name="content">Original crash log content</param>
    /// <param name="removePatterns">Patterns to remove when simplifying</param>
    /// <param name="simplifyLogs">Whether to remove lines matching patterns</param>
    /// <returns>Reformatted content</returns>
    public static string[] ReformatContent(
        string[] content,
        IList<string> removePatterns,
        bool simplifyLogs = false)
    {
        var processedLines = ProcessLinesInReverse(content, removePatterns, simplifyLogs);
        return processedLines.ToArray();
    }
}
