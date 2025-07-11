using Classic.Core.Enums;

namespace Classic.ScanLog.Models;

/// <summary>
/// Represents the result of file validation
/// </summary>
public class FileValidationResult
{
    public string FilePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public FileValidationType ValidationType { get; set; }
    public ValidationStatus Status { get; set; }
    public string Issue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Result of texture validation
/// </summary>
public class TextureValidationResult : FileValidationResult
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = string.Empty;
    public bool IsPowerOfTwo => Width > 0 && Height > 0 && 
                                (Width & (Width - 1)) == 0 && 
                                (Height & (Height - 1)) == 0;
}

/// <summary>
/// Result of archive validation
/// </summary>
public class ArchiveValidationResult : FileValidationResult
{
    public string ArchiveFormat { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public List<string> Contents { get; set; } = new();
    public long FileCount { get; set; }
    public long TotalSize { get; set; }
}

/// <summary>
/// Result of audio file validation
/// </summary>
public class AudioValidationResult : FileValidationResult
{
    public string AudioFormat { get; set; } = string.Empty;
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Result of script file validation
/// </summary>
public class ScriptValidationResult : FileValidationResult
{
    public string ScriptType { get; set; } = string.Empty;
    public string ExpectedHash { get; set; } = string.Empty;
    public string ActualHash { get; set; } = string.Empty;
    public bool IsXseScript { get; set; }
    public string ConflictingMod { get; set; } = string.Empty;
}

/// <summary>
/// Types of file validation
/// </summary>
public enum FileValidationType
{
    Texture,
    Archive,
    Audio,
    Script,
    Configuration,
    Previs,
    General
}

/// <summary>
/// Status of validation
/// </summary>
public enum ValidationStatus
{
    Valid,
    Warning,
    Error,
    Critical
}