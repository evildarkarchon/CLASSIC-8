using Classic.Core.Enums;

namespace Classic.Core.Models;

/// <summary>
/// Comprehensive configuration for crash log scanning operations
/// </summary>
public class ScanRequest
{
    // Input Configuration
    public List<string> LogFiles { get; set; } = new();
    public string OutputDirectory { get; set; } = string.Empty;
    
    // Processing Options
    public bool EnableFcxMode { get; set; }
    public bool ShowFormIdValues { get; set; }
    public bool SimplifyLogs { get; set; }
    public bool MoveUnsolvedLogs { get; set; }
    public bool EnableStatisticalLogging { get; set; }
    public ProcessingMode PreferredMode { get; set; } = ProcessingMode.Adaptive;
    public int MaxConcurrentLogs { get; set; } = Environment.ProcessorCount;
    
    // Analysis Configuration
    public bool EnablePluginAnalysis { get; set; } = true;
    public bool EnableFormIdAnalysis { get; set; } = true;
    public bool EnableSuspectScanning { get; set; } = true;
    public bool EnableRecordScanning { get; set; } = true;
    public bool EnableSettingsScanning { get; set; } = true;
    public bool EnableModDetection { get; set; } = true;
    public bool EnableGpuDetection { get; set; } = true;
    
    // Paths and Dependencies
    public string? ModsPath { get; set; }
    public string? IniPath { get; set; }
    public string? DatabasePath { get; set; }
    public string? BackupPath { get; set; }
    
    // Output Configuration
    public ReportFormat ReportFormat { get; set; } = ReportFormat.Markdown;
    public bool GenerateDetailedReports { get; set; } = true;
    public bool GenerateSummaryReport { get; set; } = true;
    public bool AutoOpenResults { get; set; } = false;
    public bool IncludeDebugInfo { get; set; } = false;
    
    // Performance Tuning
    public int BatchSize { get; set; } = 100;
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public TimeSpan StrategyEvaluationInterval { get; set; } = TimeSpan.FromSeconds(30);
    
    // Filtering Options
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public List<GameId> TargetGames { get; set; } = new();
    public List<string> ExcludePatterns { get; set; } = new();
    public List<string> IncludePatterns { get; set; } = new();
    
    // Validation
    public bool ValidateInputs { get; set; } = true;
    public bool StrictMode { get; set; } = false;
    public bool ContinueOnError { get; set; } = true;
    
    /// <summary>
    /// Creates a default scan request with sensible defaults
    /// </summary>
    public static ScanRequest CreateDefault(string outputDirectory)
    {
        return new ScanRequest
        {
            OutputDirectory = outputDirectory,
            PreferredMode = ProcessingMode.Adaptive,
            MaxConcurrentLogs = Environment.ProcessorCount,
            ReportFormat = ReportFormat.Markdown,
            GenerateDetailedReports = true,
            GenerateSummaryReport = true,
            EnablePluginAnalysis = true,
            EnableFormIdAnalysis = true,
            EnableSuspectScanning = true,
            EnableRecordScanning = true,
            EnableSettingsScanning = true,
            EnableModDetection = true,
            EnableGpuDetection = true,
            ValidateInputs = true,
            ContinueOnError = true
        };
    }
    
    /// <summary>
    /// Creates a fast scan request optimized for performance
    /// </summary>
    public static ScanRequest CreateFast(string outputDirectory)
    {
        return new ScanRequest
        {
            OutputDirectory = outputDirectory,
            PreferredMode = ProcessingMode.Parallel,
            MaxConcurrentLogs = Environment.ProcessorCount * 2,
            ReportFormat = ReportFormat.Json,
            GenerateDetailedReports = false,
            GenerateSummaryReport = true,
            EnablePluginAnalysis = true,
            EnableFormIdAnalysis = false,
            EnableSuspectScanning = true,
            EnableRecordScanning = false,
            EnableSettingsScanning = false,
            EnableModDetection = false,
            EnableGpuDetection = false,
            ValidateInputs = false,
            ContinueOnError = true,
            SimplifyLogs = true
        };
    }
    
    /// <summary>
    /// Creates a comprehensive scan request for thorough analysis
    /// </summary>
    public static ScanRequest CreateComprehensive(string outputDirectory)
    {
        return new ScanRequest
        {
            OutputDirectory = outputDirectory,
            PreferredMode = ProcessingMode.ProducerConsumer,
            MaxConcurrentLogs = Environment.ProcessorCount,
            ReportFormat = ReportFormat.Markdown,
            GenerateDetailedReports = true,
            GenerateSummaryReport = true,
            EnablePluginAnalysis = true,
            EnableFormIdAnalysis = true,
            EnableSuspectScanning = true,
            EnableRecordScanning = true,
            EnableSettingsScanning = true,
            EnableModDetection = true,
            EnableGpuDetection = true,
            EnableStatisticalLogging = true,
            IncludeDebugInfo = true,
            ValidateInputs = true,
            StrictMode = false,
            ContinueOnError = true
        };
    }
    
    /// <summary>
    /// Validates the scan request configuration
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        if (LogFiles.Count == 0)
        {
            result.AddError("No log files specified");
        }
        
        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            result.AddError("Output directory is required");
        }
        
        if (MaxConcurrentLogs <= 0)
        {
            result.AddWarning("MaxConcurrentLogs should be greater than 0, defaulting to 1");
            MaxConcurrentLogs = 1;
        }
        
        if (BatchSize <= 0)
        {
            result.AddWarning("BatchSize should be greater than 0, defaulting to 100");
            BatchSize = 100;
        }
        
        // Validate file paths exist
        foreach (var logFile in LogFiles)
        {
            if (!File.Exists(logFile))
            {
                result.AddError($"Log file not found: {logFile}");
            }
        }
        
        if (!string.IsNullOrEmpty(ModsPath) && !Directory.Exists(ModsPath))
        {
            result.AddWarning($"Mods path not found: {ModsPath}");
        }
        
        if (!string.IsNullOrEmpty(IniPath) && !Directory.Exists(IniPath))
        {
            result.AddWarning($"INI path not found: {IniPath}");
        }
        
        if (!string.IsNullOrEmpty(DatabasePath) && !Directory.Exists(DatabasePath))
        {
            result.AddWarning($"Database path not found: {DatabasePath}");
        }
        
        return result;
    }
}

public enum ReportFormat
{
    Markdown,
    Json,
    Xml,
    Html,
    Text
}

public enum ProcessingMode
{
    Sequential,      // Single-threaded processing
    Parallel,        // Task.WhenAll parallel processing
    ProducerConsumer, // Channel-based pipeline
    Adaptive         // Auto-select based on performance
}

public class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    
    public bool IsValid => Errors.Count == 0;
    public bool HasWarnings => Warnings.Count > 0;
    
    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);
    
    public string GetSummary()
    {
        if (IsValid && !HasWarnings)
            return "Validation passed";
            
        var summary = new List<string>();
        if (Errors.Count > 0)
            summary.Add($"{Errors.Count} error(s)");
        if (Warnings.Count > 0)
            summary.Add($"{Warnings.Count} warning(s)");
            
        return string.Join(", ", summary);
    }
}