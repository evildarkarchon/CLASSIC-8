using System.IO.Abstractions;
using Classic.Core.Interfaces;
using Serilog;

namespace Classic.Infrastructure.GameManagement;

/// <summary>
/// Manages game file operations including backup, restore, and removal.
/// </summary>
public class GameFileManager : IGameFileManager
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly ISettingsService _settingsService;

    // Define file patterns for each category
    private readonly Dictionary<string, string[]> _categoryFilePatterns = new()
    {
        ["XSE"] = ["*.dll", "*.exe", "*.log", "f4se_*", "skse64_*", "sksevr_*"],
        ["RESHADE"] = ["dxgi.dll", "d3d11.dll", "d3d9.dll", "opengl32.dll", "reshade.ini", "ReShade.ini"],
        ["VULKAN"] = ["vulkan-1.dll", "vulkan*.dll"],
        ["ENB"] = ["d3d11.dll", "d3d9.dll", "enbseries.ini", "enblocal.ini", "enbseries/*", "enbcache/*"]
    };

    public GameFileManager(IFileSystem fileSystem, ILogger logger, ISettingsService settingsService)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _settingsService = settingsService;
    }

    public async Task<GameFileOperationResult> BackupFilesAsync(string category,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting backup for category: {Category}", category);

        var result = new GameFileOperationResult();

        try
        {
            var gameRoot = GetGameRootDirectory();
            if (string.IsNullOrEmpty(gameRoot) || !_fileSystem.Directory.Exists(gameRoot))
                return new GameFileOperationResult
                {
                    Success = false,
                    Message = "Game root directory not found. Please configure game paths in settings."
                };

            var backupDir = GetBackupDirectory(category);
            _fileSystem.Directory.CreateDirectory(backupDir);

            var filesToBackup = GetFilesForCategory(gameRoot, category);
            if (!filesToBackup.Any())
                return new GameFileOperationResult
                {
                    Success = false,
                    Message = $"No {category} files found to backup."
                };

            var processedFiles = new List<string>();
            var errors = new List<string>();

            foreach (var sourceFile in filesToBackup)
                try
                {
                    var relativePath = _fileSystem.Path.GetRelativePath(gameRoot, sourceFile);
                    var backupFile = _fileSystem.Path.Combine(backupDir, relativePath);
                    var backupFileDir = _fileSystem.Path.GetDirectoryName(backupFile);

                    if (!string.IsNullOrEmpty(backupFileDir)) _fileSystem.Directory.CreateDirectory(backupFileDir);

                    await using var source = _fileSystem.File.OpenRead(sourceFile);
                    await using var destination = _fileSystem.File.Create(backupFile);
                    await source.CopyToAsync(destination, cancellationToken);

                    processedFiles.Add(relativePath);
                    _logger.Debug("Backed up: {File}", relativePath);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to backup {sourceFile}: {ex.Message}";
                    errors.Add(error);
                    _logger.Warning(ex, "Failed to backup file: {File}", sourceFile);
                }

            return new GameFileOperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0
                    ? $"Successfully backed up {processedFiles.Count} {category} files."
                    : $"Backed up {processedFiles.Count} files with {errors.Count} errors.",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during backup operation for category: {Category}", category);
            return new GameFileOperationResult
            {
                Success = false,
                Message = $"Backup operation failed: {ex.Message}"
            };
        }
    }

    public async Task<GameFileOperationResult> RestoreFilesAsync(string category,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting restore for category: {Category}", category);

        try
        {
            var gameRoot = GetGameRootDirectory();
            if (string.IsNullOrEmpty(gameRoot) || !_fileSystem.Directory.Exists(gameRoot))
                return new GameFileOperationResult
                {
                    Success = false,
                    Message = "Game root directory not found. Please configure game paths in settings."
                };

            var backupDir = GetBackupDirectory(category);
            if (!_fileSystem.Directory.Exists(backupDir))
                return new GameFileOperationResult
                {
                    Success = false,
                    Message = $"No backup found for {category}."
                };

            var backupFiles = _fileSystem.Directory.GetFiles(backupDir, "*", SearchOption.AllDirectories);
            if (!backupFiles.Any())
                return new GameFileOperationResult
                {
                    Success = false,
                    Message = $"Backup directory for {category} is empty."
                };

            var processedFiles = new List<string>();
            var errors = new List<string>();

            foreach (var backupFile in backupFiles)
                try
                {
                    var relativePath = _fileSystem.Path.GetRelativePath(backupDir, backupFile);
                    var targetFile = _fileSystem.Path.Combine(gameRoot, relativePath);
                    var targetDir = _fileSystem.Path.GetDirectoryName(targetFile);

                    if (!string.IsNullOrEmpty(targetDir)) _fileSystem.Directory.CreateDirectory(targetDir);

                    await using var source = _fileSystem.File.OpenRead(backupFile);
                    await using var destination = _fileSystem.File.Create(targetFile);
                    await source.CopyToAsync(destination, cancellationToken);

                    processedFiles.Add(relativePath);
                    _logger.Debug("Restored: {File}", relativePath);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to restore {backupFile}: {ex.Message}";
                    errors.Add(error);
                    _logger.Warning(ex, "Failed to restore file: {File}", backupFile);
                }

            return new GameFileOperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0
                    ? $"Successfully restored {processedFiles.Count} {category} files."
                    : $"Restored {processedFiles.Count} files with {errors.Count} errors.",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during restore operation for category: {Category}", category);
            return new GameFileOperationResult
            {
                Success = false,
                Message = $"Restore operation failed: {ex.Message}"
            };
        }
    }

    public Task<GameFileOperationResult> RemoveFilesAsync(string category,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting remove operation for category: {Category}", category);

        try
        {
            var gameRoot = GetGameRootDirectory();
            if (string.IsNullOrEmpty(gameRoot) || !_fileSystem.Directory.Exists(gameRoot))
                return Task.FromResult(new GameFileOperationResult
                {
                    Success = false,
                    Message = "Game root directory not found. Please configure game paths in settings."
                });

            var filesToRemove = GetFilesForCategory(gameRoot, category);
            if (!filesToRemove.Any())
                return Task.FromResult(new GameFileOperationResult
                {
                    Success = true,
                    Message = $"No {category} files found to remove."
                });

            var processedFiles = new List<string>();
            var errors = new List<string>();

            foreach (var file in filesToRemove)
                try
                {
                    var relativePath = _fileSystem.Path.GetRelativePath(gameRoot, file);

                    // Remove read-only attribute if present
                    var fileInfo = _fileSystem.FileInfo.New(file);
                    if (fileInfo.Exists && fileInfo.IsReadOnly) fileInfo.IsReadOnly = false;

                    _fileSystem.File.Delete(file);
                    processedFiles.Add(relativePath);
                    _logger.Debug("Removed: {File}", relativePath);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to remove {file}: {ex.Message}";
                    errors.Add(error);
                    _logger.Warning(ex, "Failed to remove file: {File}", file);
                }

            return Task.FromResult(new GameFileOperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0
                    ? $"Successfully removed {processedFiles.Count} {category} files."
                    : $"Removed {processedFiles.Count} files with {errors.Count} errors.",
                ProcessedFiles = processedFiles,
                Errors = errors
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during remove operation for category: {Category}", category);
            return Task.FromResult(new GameFileOperationResult
            {
                Success = false,
                Message = $"Remove operation failed: {ex.Message}"
            });
        }
    }

    public bool HasBackup(string category)
    {
        var backupDir = GetBackupDirectory(category);
        return _fileSystem.Directory.Exists(backupDir) &&
               _fileSystem.Directory.GetFiles(backupDir, "*", SearchOption.AllDirectories).Any();
    }

    public string GetBackupDirectory(string category)
    {
        var baseBackupDir = _fileSystem.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "CLASSIC Data",
            "Backups",
            category);
        return baseBackupDir;
    }

    private string GetGameRootDirectory()
    {
        // Try to get from settings first
        var settings = _settingsService.Settings;

        // For now, return empty - this should be configured via settings
        // In a real implementation, this would come from game detection or user configuration
        return string.Empty;
    }

    private List<string> GetFilesForCategory(string gameRoot, string category)
    {
        var files = new List<string>();

        if (!_categoryFilePatterns.TryGetValue(category.ToUpperInvariant(), out var patterns))
        {
            _logger.Warning("Unknown category: {Category}", category);
            return files;
        }

        foreach (var pattern in patterns)
            try
            {
                var matchingFiles = _fileSystem.Directory.GetFiles(gameRoot, pattern, SearchOption.TopDirectoryOnly);
                files.AddRange(matchingFiles);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error searching for pattern {Pattern} in {GameRoot}", pattern, gameRoot);
            }

        return files.Distinct().ToList();
    }
}
