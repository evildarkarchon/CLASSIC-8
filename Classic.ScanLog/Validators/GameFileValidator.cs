using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace Classic.ScanLog.Validators;

/// <summary>
/// Main game file validator that coordinates all validation types
/// </summary>
public class GameFileValidator
{
    private readonly IFileSystem _fileSystem;
    private readonly TextureValidator _textureValidator;
    private readonly ArchiveValidator _archiveValidator;
    private readonly AudioValidator _audioValidator;
    private readonly ScriptValidator _scriptValidator;
    private readonly ILogger<GameFileValidator> _logger;

    // File extensions for different validation types
    private static readonly HashSet<string> TextureExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dds", ".tga", ".png", ".jpg", ".jpeg", ".bmp"
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ba2", ".bsa"
    };

    // Files to skip during validation
    private static readonly HashSet<string> SkipFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "thumbs.db", "desktop.ini", ".ds_store", "readme.txt", "changelog.txt"
    };

    public GameFileValidator(
        IFileSystem fileSystem,
        TextureValidator textureValidator,
        ArchiveValidator archiveValidator,
        AudioValidator audioValidator,
        ScriptValidator scriptValidator,
        ILogger<GameFileValidator> logger)
    {
        _fileSystem = fileSystem;
        _textureValidator = textureValidator;
        _archiveValidator = archiveValidator;
        _audioValidator = audioValidator;
        _scriptValidator = scriptValidator;
        _logger = logger;
    }

    /// <summary>
    /// Validates all files in a directory recursively
    /// </summary>
    public async Task<GameFileValidationSummary> ValidateDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        var summary = new GameFileValidationSummary
        {
            DirectoryPath = directoryPath,
            StartTime = DateTime.Now
        };

        try
        {
            if (!_fileSystem.Directory.Exists(directoryPath))
            {
                summary.Errors.Add($"Directory not found: {directoryPath}");
                return summary;
            }

            _logger.LogInformation("Starting file validation for directory: {DirectoryPath}", directoryPath);

            // Get all files recursively
            var allFiles = GetAllFiles(directoryPath);
            summary.TotalFilesScanned = allFiles.Count;

            _logger.LogDebug("Found {FileCount} files to validate", allFiles.Count);

            // Group files by validation type for parallel processing
            var textureFiles = allFiles
                .Where(f => IsTextureFile(f.filePath))
                .ToList();

            var archiveFiles = allFiles
                .Where(f => ArchiveValidator.IsSupportedArchive(f.filePath))
                .ToList();

            var audioFiles = allFiles
                .Where(f => AudioValidator.IsAudioFile(f.filePath))
                .ToList();

            var scriptFiles = allFiles
                .Where(f => ScriptValidator.IsScriptFile(f.filePath))
                .ToList();

            // Run validations in parallel
            var validationTasks = new List<Task>
            {
                ValidateTextureFilesAsync(summary, textureFiles, cancellationToken),
                ValidateArchiveFilesAsync(summary, archiveFiles, cancellationToken),
                ValidateAudioFilesAsync(summary, audioFiles, cancellationToken),
                ValidateScriptFilesAsync(summary, scriptFiles, cancellationToken)
            };

            await Task.WhenAll(validationTasks);

            // Check for special cases
            await ValidateSpecialCasesAsync(summary, allFiles, cancellationToken);

            summary.EndTime = DateTime.Now;
            summary.Success = summary.Errors.Count == 0;

            _logger.LogInformation(
                "File validation completed for {DirectoryPath}: {TotalFiles} files, " +
                "{Issues} issues found in {Duration:mm\\:ss}",
                directoryPath, summary.TotalFilesScanned, summary.TotalIssues, summary.Duration);
        }
        catch (Exception ex)
        {
            summary.Errors.Add($"Validation failed: {ex.Message}");
            summary.EndTime = DateTime.Now;
            _logger.LogError(ex, "Error during file validation for directory: {DirectoryPath}", directoryPath);
        }

        return summary;
    }

    /// <summary>
    /// Validates a single file
    /// </summary>
    public async Task<FileValidationResult> ValidateFileAsync(
        string filePath,
        string? relativePath = null,
        CancellationToken cancellationToken = default)
    {
        relativePath ??= Path.GetFileName(filePath);

        try
        {
            if (IsTextureFile(filePath))
                return await _textureValidator.ValidateTextureAsync(filePath, relativePath, cancellationToken);

            if (ArchiveValidator.IsSupportedArchive(filePath))
                return await _archiveValidator.ValidateArchiveAsync(filePath, relativePath, cancellationToken);

            if (AudioValidator.IsAudioFile(filePath))
                return await _audioValidator.ValidateAudioAsync(filePath, relativePath, cancellationToken);

            if (ScriptValidator.IsScriptFile(filePath))
                return await _scriptValidator.ValidateScriptAsync(filePath, relativePath, cancellationToken);

            // General file validation
            return new FileValidationResult
            {
                FilePath = filePath,
                RelativePath = relativePath,
                ValidationType = FileValidationType.General,
                Status = ValidationStatus.Valid,
                Description = $"File type: {Path.GetExtension(filePath).ToUpperInvariant()}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file: {FilePath}", filePath);
            return new FileValidationResult
            {
                FilePath = filePath,
                RelativePath = relativePath,
                ValidationType = FileValidationType.General,
                Status = ValidationStatus.Error,
                Issue = "Validation failed",
                Description = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets all files in directory recursively
    /// </summary>
    private List<(string filePath, string relativePath)> GetAllFiles(string directoryPath)
    {
        var files = new List<(string, string)>();
        var baseDirectory = new DirectoryInfo(directoryPath);

        try
        {
            foreach (var file in _fileSystem.Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);

                // Skip unwanted files
                if (SkipFiles.Contains(fileName))
                    continue;

                var relativePath = Path.GetRelativePath(directoryPath, file);
                files.Add((file, relativePath));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files from directory: {DirectoryPath}", directoryPath);
        }

        return files;
    }

    /// <summary>
    /// Validates texture files
    /// </summary>
    private async Task ValidateTextureFilesAsync(
        GameFileValidationSummary summary,
        List<(string filePath, string relativePath)> textureFiles,
        CancellationToken cancellationToken)
    {
        if (!textureFiles.Any()) return;

        try
        {
            var results = await _textureValidator.ValidateTexturesAsync(textureFiles, cancellationToken);
            summary.TextureResults.AddRange(results);
        }
        catch (Exception ex)
        {
            summary.Errors.Add($"Texture validation failed: {ex.Message}");
            _logger.LogError(ex, "Error during texture validation");
        }
    }

    /// <summary>
    /// Validates archive files
    /// </summary>
    private async Task ValidateArchiveFilesAsync(
        GameFileValidationSummary summary,
        List<(string filePath, string relativePath)> archiveFiles,
        CancellationToken cancellationToken)
    {
        if (!archiveFiles.Any()) return;

        try
        {
            var results = await _archiveValidator.ValidateArchivesAsync(archiveFiles, cancellationToken);
            summary.ArchiveResults.AddRange(results);
        }
        catch (Exception ex)
        {
            summary.Errors.Add($"Archive validation failed: {ex.Message}");
            _logger.LogError(ex, "Error during archive validation");
        }
    }

    /// <summary>
    /// Validates audio files
    /// </summary>
    private async Task ValidateAudioFilesAsync(
        GameFileValidationSummary summary,
        List<(string filePath, string relativePath)> audioFiles,
        CancellationToken cancellationToken)
    {
        if (!audioFiles.Any()) return;

        try
        {
            var results = await _audioValidator.ValidateAudioFilesAsync(audioFiles, cancellationToken);
            summary.AudioResults.AddRange(results);
        }
        catch (Exception ex)
        {
            summary.Errors.Add($"Audio validation failed: {ex.Message}");
            _logger.LogError(ex, "Error during audio validation");
        }
    }

    /// <summary>
    /// Validates script files
    /// </summary>
    private async Task ValidateScriptFilesAsync(
        GameFileValidationSummary summary,
        List<(string filePath, string relativePath)> scriptFiles,
        CancellationToken cancellationToken)
    {
        if (!scriptFiles.Any()) return;

        try
        {
            var results = await _scriptValidator.ValidateScriptsAsync(scriptFiles, cancellationToken);
            summary.ScriptResults.AddRange(results);
        }
        catch (Exception ex)
        {
            summary.Errors.Add($"Script validation failed: {ex.Message}");
            _logger.LogError(ex, "Error during script validation");
        }
    }

    /// <summary>
    /// Validates special cases (previs files, etc.)
    /// </summary>
    private async Task ValidateSpecialCasesAsync(
        GameFileValidationSummary summary,
        List<(string filePath, string relativePath)> allFiles,
        CancellationToken cancellationToken)
    {
        // Check for previs/precombine files
        var previsFiles = allFiles
            .Where(f => f.filePath.EndsWith(".uvd", StringComparison.OrdinalIgnoreCase) ||
                        f.filePath.EndsWith("_oc.nif", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (previsFiles.Any())
        {
            var previsResult = new FileValidationResult
            {
                FilePath = previsFiles.First().filePath,
                RelativePath = previsFiles.First().relativePath,
                ValidationType = FileValidationType.Previs,
                Status = ValidationStatus.Warning,
                Issue = "Previs/Precombine files detected",
                Description = $"Found {previsFiles.Count} previs files that may require Previs Repair Pack (PRP)",
                Recommendation = "Ensure Previs Repair Pack is installed and properly positioned in load order"
            };

            summary.SpecialResults.Add(previsResult);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks if a file is a texture file
    /// </summary>
    private static bool IsTextureFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return TextureExtensions.Contains(extension);
    }
}

/// <summary>
/// Summary of game file validation results
/// </summary>
public class GameFileValidationSummary
{
    public string DirectoryPath { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool Success { get; set; }

    public int TotalFilesScanned { get; set; }
    public List<string> Errors { get; set; } = new();

    public List<TextureValidationResult> TextureResults { get; set; } = new();
    public List<ArchiveValidationResult> ArchiveResults { get; set; } = new();
    public List<AudioValidationResult> AudioResults { get; set; } = new();
    public List<ScriptValidationResult> ScriptResults { get; set; } = new();
    public List<FileValidationResult> SpecialResults { get; set; } = new();

    public int TotalIssues => TextureResults.Count(r => r.Status != ValidationStatus.Valid) +
                              ArchiveResults.Count(r => r.Status != ValidationStatus.Valid) +
                              AudioResults.Count(r => r.Status != ValidationStatus.Valid) +
                              ScriptResults.Count(r => r.Status != ValidationStatus.Valid) +
                              SpecialResults.Count(r => r.Status != ValidationStatus.Valid);

    public int CriticalIssues => GetIssueCount(ValidationStatus.Critical);
    public int ErrorIssues => GetIssueCount(ValidationStatus.Error);
    public int WarningIssues => GetIssueCount(ValidationStatus.Warning);

    private int GetIssueCount(ValidationStatus status)
    {
        return TextureResults.Count(r => r.Status == status) +
               ArchiveResults.Count(r => r.Status == status) +
               AudioResults.Count(r => r.Status == status) +
               ScriptResults.Count(r => r.Status == status) +
               SpecialResults.Count(r => r.Status == status);
    }
}
