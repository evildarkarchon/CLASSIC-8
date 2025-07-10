namespace Classic.Core.Models;

public class Plugin
{
    public string Name { get; set; } = string.Empty;
    public string LoadOrder { get; set; } = string.Empty;
    public bool IsLightPlugin { get; set; }
    public PluginStatus Status { get; set; }
}

public enum PluginStatus
{
    Active,
    Inactive,
    Missing,
    Error
}
