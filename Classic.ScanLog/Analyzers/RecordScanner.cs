using Classic.Core.Interfaces;
using Classic.Core.Models;
using Classic.ScanLog.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Classic.ScanLog.Analyzers;

/// <summary>
/// Scans crash logs for named records and extracts relevant information.
/// Ported from Python RecordScanner implementation.
/// </summary>
public class RecordScanner : IRecordScanner
{
    private readonly ScanLogConfiguration _configuration;
    private readonly HashSet<string> _lowerRecords;
    private readonly HashSet<string> _lowerIgnore;
    private const string RspMarker = "[RSP+";
    private const int RspOffset = 30;

    public RecordScanner(ScanLogConfiguration configuration)
    {
        _configuration = configuration;
        _lowerRecords = new HashSet<string>(
            (_configuration.RecordsToDetect ?? Enumerable.Empty<string>()).Select(r => r.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
        _lowerIgnore = new HashSet<string>(
            (_configuration.RecordsToIgnore ?? Enumerable.Empty<string>()).Select(r => r.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IEnumerable<Suspect> Scan(CrashLog crashLog)
    {
        var namedRecords = ScanNamedRecords(crashLog);

        if (!namedRecords.Any()) yield break;

        // Create a single suspect for all found records
        yield return new Suspect
        {
            Name = "Named Records Found",
            Description = $"Found {namedRecords.Count} unique named records in crash log",
            Type = SuspectType.Unknown,
            SeverityScore = 2, // Low severity - informational
            Evidence = string.Join(", ", namedRecords.Keys.Take(5)) + (namedRecords.Count > 5 ? "..." : ""),
            Recommendation = "Check the named records for clues about problematic game objects, mods, or files.",
            RelatedFiles = namedRecords.Keys.Where(r => r.Contains(".")).Take(10).ToList()
        };
    }

    /// <summary>
    /// Scans for named records in the crash log's call stack.
    /// </summary>
    /// <param name="crashLog">The crash log to scan</param>
    /// <returns>Dictionary of found records with their occurrence counts</returns>
    public Dictionary<string, int> ScanNamedRecords(CrashLog crashLog)
    {
        var recordMatches = new List<string>();

        // Get the call stack segment
        if (crashLog.Segments.TryGetValue("PROBABLE CALL STACK", out var callStack) ||
            crashLog.Segments.TryGetValue("STACK", out callStack))
            FindMatchingRecords(callStack, recordMatches);

        // Count occurrences and return sorted results
        var recordCounts = new Dictionary<string, int>();
        foreach (var record in recordMatches) recordCounts[record] = recordCounts.GetValueOrDefault(record, 0) + 1;

        return recordCounts.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Extracts records from a call stack segment without filtering.
    /// </summary>
    /// <param name="callStackLines">The call stack lines to process</param>
    /// <returns>List of extracted records</returns>
    public List<string> ExtractRecords(IEnumerable<string> callStackLines)
    {
        var recordMatches = new List<string>();
        FindMatchingRecords(callStackLines.ToList(), recordMatches);
        return recordMatches;
    }

    /// <summary>
    /// Finds and collects matching records from call stack lines.
    /// </summary>
    /// <param name="callStackLines">Lines to search through</param>
    /// <param name="recordMatches">Collection to add matches to</param>
    private void FindMatchingRecords(IEnumerable<string> callStackLines, ICollection<string> recordMatches)
    {
        foreach (var line in callStackLines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var lowerLine = line.ToLowerInvariant();

            // Check if line contains any target record and doesn't contain any ignored terms
            var hasTargetRecord = _lowerRecords.Any(record => lowerLine.Contains(record));
            var hasIgnoredRecord = _lowerIgnore.Any(ignored => lowerLine.Contains(ignored));

            if (hasTargetRecord && !hasIgnoredRecord)
            {
                // Extract the relevant part of the line based on format
                string extractedRecord;
                if (line.Contains(RspMarker) && line.Length > RspOffset)
                    extractedRecord = line[RspOffset..].Trim();
                else
                    extractedRecord = line.Trim();

                if (!string.IsNullOrWhiteSpace(extractedRecord)) recordMatches.Add(extractedRecord);
            }
        }
    }

    /// <summary>
    /// Generates a formatted report of found records.
    /// </summary>
    /// <param name="recordCounts">Dictionary of records and their counts</param>
    /// <param name="crashGenName">Name of the crash generator (e.g., "Buffout 4")</param>
    /// <returns>Formatted report string</returns>
    public string GenerateRecordReport(Dictionary<string, int> recordCounts, string crashGenName = "Buffout 4")
    {
        if (!recordCounts.Any()) return "* COULDN'T FIND ANY NAMED RECORDS *";

        var report = new System.Text.StringBuilder();

        foreach (var (record, count) in recordCounts) report.AppendLine($"- {record} | {count}");

        report.AppendLine();
        report.AppendLine("[Last number counts how many times each Named Record shows up in the crash log.]");
        report.AppendLine(
            $"These records were caught by {crashGenName} and some of them might be related to this crash.");
        report.AppendLine("Named records should give extra info on involved game objects, record types or mod files.");

        return report.ToString();
    }
}
