using YamlDotNet.Serialization;

namespace Classic.ScanLog.Models;

/// <summary>
/// Represents the mod conflict database structure from YAML
/// </summary>
public class ModConflictDatabase
{
    [YamlMember(Alias = "mods_core")]
    public Dictionary<string, object> ModsCore { get; set; } = new();

    [YamlMember(Alias = "mods_freq")]
    public Dictionary<string, object> ModsFreq { get; set; } = new();

    [YamlMember(Alias = "mods_conf")]
    public Dictionary<string, object> ModsConf { get; set; } = new();

    [YamlMember(Alias = "mods_solu")]
    public Dictionary<string, object> ModsSolu { get; set; } = new();

    [YamlMember(Alias = "gpu_compatibility")]
    public GpuCompatibility? GpuCompatibility { get; set; }

    [YamlMember(Alias = "load_order_warnings")]
    public Dictionary<string, string> LoadOrderWarnings { get; set; } = new();
}

/// <summary>
/// GPU-specific compatibility information
/// </summary>
public class GpuCompatibility
{
    [YamlMember(Alias = "nvidia_specific")]
    public List<string> NvidiaSpecific { get; set; } = new();

    [YamlMember(Alias = "amd_specific")]
    public List<string> AmdSpecific { get; set; } = new();
}

/// <summary>
/// Represents a mod entry with optional GPU constraints
/// </summary>
public class ModEntry
{
    [YamlMember(Alias = "gpu_constraint")]
    public string? GpuConstraint { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a detected mod conflict
/// </summary>
public class ModConflictResult
{
    public string ModName { get; set; } = string.Empty;
    public string PluginId { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public ConflictSeverity Severity { get; set; }
    public ConflictType Type { get; set; }
    public string? GpuSpecific { get; set; }
}

/// <summary>
/// Types of mod conflicts
/// </summary>
public enum ConflictType
{
    FrequentCrash,
    ModPairConflict,
    MissingImportant,
    GpuIncompatible,
    LoadOrderIssue,
    HasSolution
}

/// <summary>
/// Severity levels for mod conflicts
/// </summary>
public enum ConflictSeverity
{
    Info = 1,
    Warning = 2,
    Caution = 3,
    Critical = 4,
    Severe = 5
}

/// <summary>
/// GPU information extracted from crash logs
/// </summary>
public class GpuInfo
{
    public string PrimaryGpu { get; set; } = string.Empty;
    public string SecondaryGpu { get; set; } = string.Empty;
    public GpuManufacturer Manufacturer { get; set; }
    public GpuManufacturer RivalManufacturer { get; set; }
}

/// <summary>
/// GPU manufacturers for compatibility checks
/// </summary>
public enum GpuManufacturer
{
    Unknown,
    Nvidia,
    Amd,
    Intel
}

