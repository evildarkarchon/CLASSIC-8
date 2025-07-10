using Classic.Core.Models;

namespace Classic.Core.Interfaces;

public interface ICrashLogParser
{
    Task<CrashLog> ParseCrashLogAsync(string filePath, CancellationToken cancellationToken = default);
    Task<CrashLog> ParseCrashLogFromContentAsync(string[] content, string fileName, CancellationToken cancellationToken = default);
    Task<Dictionary<string, List<string>>> ExtractSegmentsAsync(string[] content, CancellationToken cancellationToken = default);
}
