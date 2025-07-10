using System.Security.Cryptography;

namespace Classic.Infrastructure.IO;

public static class FileUtilities
{
    public static async Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
    {
        var lines = new List<string>();
        using var reader = new StreamReader(path);
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lines.Add(line);
        }
        return lines.ToArray();
    }

    public static async Task<string> CalculateFileHashAsync(string path, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(path);
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public static bool IsValidCrashLog(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".txt" || extension == ".log";
    }

    public static async Task<FileInfo[]> GetCrashLogsAsync(string directory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<FileInfo>();

        var directoryInfo = new DirectoryInfo(directory);
        var files = directoryInfo.GetFiles("*.txt", SearchOption.TopDirectoryOnly)
            .Concat(directoryInfo.GetFiles("*.log", SearchOption.TopDirectoryOnly))
            .Where(f => IsValidCrashLog(f.FullName))
            .OrderByDescending(f => f.LastWriteTime)
            .ToArray();

        return files;
    }
}
