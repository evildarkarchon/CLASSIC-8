namespace Classic.ScanLog.Models;

/// <summary>
///     Configuration settings for crash log scanning operations.
///     Equivalent to Python's ClassicScanLogsInfo.
/// </summary>
public class ScanLogConfiguration
{
    public string LogFolderPath { get; set; } = string.Empty;
    public string OutputFolderPath { get; set; } = string.Empty;
    public bool EnableFcxMode { get; set; }
    public bool AutoOpenResults { get; set; }
    public bool PromptUpload { get; set; }
    public int MaxConcurrentLogs { get; set; } = Environment.ProcessorCount;
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public ProcessingMode PreferredMode { get; set; } = ProcessingMode.Adaptive;
    public int BatchSize { get; set; } = 100;
    public TimeSpan StrategyEvaluationInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Regex patterns for parsing crash logs
    /// </summary>
    public ScanLogPatterns Patterns { get; set; } = new();

    /// <summary>
    ///     Known crash suspects database
    /// </summary>
    public Dictionary<string, CrashSuspect> CrashSuspects { get; set; } = new();

    /// <summary>
    ///     Plugin analysis configuration
    /// </summary>
    public PluginAnalysisConfiguration PluginAnalysis { get; set; } = new();

    /// <summary>
    ///     Records to detect in crash logs (from catch_log_records)
    /// </summary>
    public List<string> RecordsToDetect { get; set; } = new()
    {
        ".bgsm", ".bto", ".btr", ".dds", ".dll+", ".fuz", ".hkb", ".hkx", ".ini", ".nif", 
        ".pex", ".strings", ".swf", ".txt", ".uvd", ".wav", ".xwm", "data/", "data\\", 
        "scaleform", "editorid:", "file:", "function:", "name:"
    };

    /// <summary>
    ///     Records to ignore/exclude in crash logs (from Crashlog_Records_Exclude)
    /// </summary>
    public List<string> RecordsToIgnore { get; set; } = new()
    {
        "\"\"", "...", "FE:", ".esl", ".esm", ".esp", ".exe", "Buffout4.dll+", "KERNEL", 
        "MSVC", "USER32", "Unhandled", "cudart64_75.dll+", "d3d11.dll+", "dxgi.dll+", 
        "f4se", "flexRelease_x64.dll+", "kernel32.dll+", "ntdll", "nvcuda64.dll+"
    };

    /// <summary>
    ///     List of plugins to ignore during analysis
    /// </summary>
    public List<string> IgnorePluginsList { get; set; } = new();
}

public class ScanLogPatterns
{
    public string GameVersionPattern { get; set; } = "^(.+?) v(.+?)$";
    public string BuffoutVersionPattern { get; set; } = @"Buffout 4 v(.+?)(?:\s|$)";
    public string MainErrorPattern { get; set; } = @"Unhandled exception ""(.+?)"" at (.+?)(?:\s|$)";
    public string PluginSectionPattern { get; set; } = "^PLUGINS:$";
    public string F4SeSectionPattern { get; set; } = "^F4SE PLUGINS:$";
    public string StackSectionPattern { get; set; } = "^PROBABLE CALL STACK:$";
    public string SystemSpecsPattern { get; set; } = "^SYSTEM SPECS:$";
    public string RegistersPattern { get; set; } = "^REGISTERS:$";
    public string StackPattern { get; set; } = "^STACK:$";
    public string ModulesPattern { get; set; } = "^MODULES:$";

    // Plugin parsing patterns
    public string PluginEntryPattern { get; set; } = @"^\[([0-9A-F]{2})\]\s+(.+?)(?:\s+\[(.+?)\])?$";
    public string PluginCountPattern { get; set; } = @"Light:\s*(\d+)\s+Regular:\s*(\d+)\s+Total:\s*(\d+)";

    // Form ID patterns
    public string FormIdPattern { get; set; } = @"FormID:\s*0x([0-9A-F]{8})";
    public string FormTypePattern { get; set; } = @"FormType:\s*(\w+)\s*\((\d+)\)";

    // Call stack patterns
    public string CallStackEntryPattern { get; set; } = @"^\[\s*(\d+)\]\s+0x([0-9A-F]+)\s+(.+)$";

    // System specs patterns
    public string OsPattern { get; set; } = @"OS:\s*(.+)$";
    public string CpuPattern { get; set; } = @"CPU:\s*(.+)$";
    public string GpuPattern { get; set; } = @"GPU #(\d+):\s*(.+)$";
    public string MemoryPattern { get; set; } = @"PHYSICAL MEMORY:\s*(.+?)/(.+?)$";
}

public class CrashSuspect
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Severity { get; set; }
    public List<string> Patterns { get; set; } = new();
    public List<string> Solutions { get; set; } = new();
    public string DocumentationUrl { get; set; } = string.Empty;
}

public class PluginAnalysisConfiguration
{
    public bool CheckForProblematicMods { get; set; } = true;
    public bool CheckForConflicts { get; set; } = true;
    public bool CheckForPatches { get; set; } = true;
    public bool ValidateLoadOrder { get; set; } = true;
    public int MaxPluginCount { get; set; } = 255;
    public int RecommendedMaxPlugins { get; set; } = 200;

    /// <summary>
    ///     Known problematic mods database
    /// </summary>
    public Dictionary<string, ProblematicMod> ProblematicMods { get; set; } = new();

    /// <summary>
    ///     Known mod conflicts database
    /// </summary>
    public List<ModConflict> ModConflicts { get; set; } = new();

    /// <summary>
    ///     Community patches database
    /// </summary>
    public Dictionary<string, CommunityPatch> CommunityPatches { get; set; } = new();
}

public class ProblematicMod
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public int Severity { get; set; }
    public List<string> Solutions { get; set; } = new();
    public string AlternativeRecommendation { get; set; } = string.Empty;
}

public class ModConflict
{
    public List<string> ConflictingMods { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public int Severity { get; set; }
}

public class CommunityPatch
{
    public string TargetMod { get; set; } = string.Empty;
    public string PatchName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}

public enum ProcessingMode
{
    Parallel, // Task.WhenAll parallel processing
    ProducerConsumer, // Channel-based pipeline
    Adaptive // Auto-select based on performance
}

public enum ProcessingStrategy
{
    Parallel,
    ProducerConsumer
}
