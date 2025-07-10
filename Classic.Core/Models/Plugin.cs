namespace Classic.Core.Models;

public class Plugin
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int LoadOrder { get; set; }
    public bool IsLight { get; set; }
    public bool IsMaster { get; set; }
    public PluginStatus Status { get; set; }
    public string Flags { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public enum PluginStatus
{
    Active,
    Inactive,
    Missing,
    Error,
    Light,
    Master,
    Regular,
    Unknown
}
