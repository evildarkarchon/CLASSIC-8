namespace Classic.Core.Models;

public class CrashLog
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public string CrashGenVersion { get; set; } = string.Empty;
    public string MainError { get; set; } = string.Empty;
    public List<string> RawContent { get; set; } = new();
    public Dictionary<string, List<string>> Segments { get; set; } = new();
}
