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
    public List<PluginInfo> Plugins { get; set; } = new();
    public DateTime Timestamp => DateCreated;
    public List<Suspect> Suspects { get; set; } = new();
}

/// <summary>
/// Plugin information from crash logs
/// </summary>
public class PluginInfo
{
    public string FileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEsl { get; set; }
    public bool IsEsm { get; set; }
    public int LoadOrder { get; set; }
    public bool HasPluginLimit { get; set; }
}
