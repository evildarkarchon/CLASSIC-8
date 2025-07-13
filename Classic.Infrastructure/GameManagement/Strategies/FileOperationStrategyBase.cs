using System.IO.Abstractions;
using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Serilog;

namespace Classic.Infrastructure.GameManagement.Strategies;

/// <summary>
/// Base implementation for file operation strategies containing common logic.
/// </summary>
public abstract class FileOperationStrategyBase : IFileOperationStrategy
{
    protected readonly IFileSystem FileSystem;
    protected readonly ILogger Logger;

    protected FileOperationStrategyBase(IFileSystem fileSystem, ILogger logger)
    {
        FileSystem = fileSystem;
        Logger = logger;
    }

    public abstract string Category { get; }
    public abstract string[] FilePatterns { get; }

    public virtual async Task<GameFileOperationResult> ExecuteAsync(
        GameFileOperation operation,
        string gameRoot,
        string backupDir,
        CancellationToken cancellationToken = default)
    {
        return operation switch
        {
            GameFileOperation.Backup => await BackupFilesAsync(gameRoot, backupDir, cancellationToken).ConfigureAwait(false),
            GameFileOperation.Restore => await RestoreFilesAsync(gameRoot, backupDir, cancellationToken).ConfigureAwait(false),
            GameFileOperation.Remove => await RemoveFilesAsync(gameRoot, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation")
        };
    }

    protected virtual async Task<GameFileOperationResult> BackupFilesAsync(
        string gameRoot,
        string backupDir,
        CancellationToken cancellationToken)
    {
        Logger.Information("Starting backup for category: {Category}", Category);

        try
        {
            FileSystem.Directory.CreateDirectory(backupDir);

            var filesToBackup = GetFilesForCategory(gameRoot);
            if (!filesToBackup.Any())
            {
                return new GameFileOperationResult
                {
                    Success = false,
                    Message = $"No {Category} files found to backup."
                };
            }

            var processedFiles = new List<string>();
            var errors = new List<string>();

            foreach (var sourceFile in filesToBackup)
            {
                try
                {
                    var relativePath = FileSystem.Path.GetRelativePath(gameRoot, sourceFile);
                    var backupFile = FileSystem.Path.Combine(backupDir, relativePath);
                    var backupFileDir = FileSystem.Path.GetDirectoryName(backupFile);

                    if (!string.IsNullOrEmpty(backupFileDir))
                        FileSystem.Directory.CreateDirectory(backupFileDir);

                    await using var source = FileSystem.File.OpenRead(sourceFile);
                    await using var destination = FileSystem.File.Create(backupFile);
                    await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

                    processedFiles.Add(relativePath);
                    Logger.Debug("Backed up: {File}", relativePath);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to backup {sourceFile}: {ex.Message}";
                    errors.Add(error);
                    Logger.Warning(ex, "Failed to backup file: {File}", sourceFile);
                }
            }

            return new GameFileOperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0
                    ? $"Successfully backed up {processedFiles.Count} {Category} files."
                    : $"Backed up {processedFiles.Count} files with {errors.Count} errors.",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during backup operation for category: {Category}", Category);
            return new GameFileOperationResult
            {
                Success = false,
                Message = $"Backup operation failed: {ex.Message}"
            };
        }
    }

    protected virtual async Task<GameFileOperationResult> RestoreFilesAsync(
        string gameRoot,
        string backupDir,
        CancellationToken cancellationToken)
    {
        Logger.Information("Starting restore for category: {Category}", Category);

        try
        {
            if (!FileSystem.Directory.Exists(backupDir))
            {
                return new GameFileOperationResult
                {
                    Success = false,
                    Message = $"No backup found for {Category}."
                };
            }

            var backupFiles = FileSystem.Directory.GetFiles(backupDir, "*", SearchOption.AllDirectories);
            if (!backupFiles.Any())
            {
                return new GameFileOperationResult
                {
                    Success = false,
                    Message = $"Backup directory for {Category} is empty."
                };
            }

            var processedFiles = new List<string>();
            var errors = new List<string>();

            foreach (var backupFile in backupFiles)
            {
                try
                {
                    var relativePath = FileSystem.Path.GetRelativePath(backupDir, backupFile);
                    var targetFile = FileSystem.Path.Combine(gameRoot, relativePath);
                    var targetDir = FileSystem.Path.GetDirectoryName(targetFile);

                    if (!string.IsNullOrEmpty(targetDir))
                        FileSystem.Directory.CreateDirectory(targetDir);

                    await using var source = FileSystem.File.OpenRead(backupFile);
                    await using var destination = FileSystem.File.Create(targetFile);
                    await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

                    processedFiles.Add(relativePath);
                    Logger.Debug("Restored: {File}", relativePath);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to restore {backupFile}: {ex.Message}";
                    errors.Add(error);
                    Logger.Warning(ex, "Failed to restore file: {File}", backupFile);
                }
            }

            return new GameFileOperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0
                    ? $"Successfully restored {processedFiles.Count} {Category} files."
                    : $"Restored {processedFiles.Count} files with {errors.Count} errors.",
                ProcessedFiles = processedFiles,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during restore operation for category: {Category}", Category);
            return new GameFileOperationResult
            {
                Success = false,
                Message = $"Restore operation failed: {ex.Message}"
            };
        }
    }

    protected virtual Task<GameFileOperationResult> RemoveFilesAsync(
        string gameRoot,
        CancellationToken cancellationToken)
    {
        Logger.Information("Starting remove operation for category: {Category}", Category);

        try
        {
            var filesToRemove = GetFilesForCategory(gameRoot);
            if (!filesToRemove.Any())
            {
                return Task.FromResult(new GameFileOperationResult
                {
                    Success = true,
                    Message = $"No {Category} files found to remove."
                });
            }

            var processedFiles = new List<string>();
            var errors = new List<string>();

            foreach (var file in filesToRemove)
            {
                try
                {
                    var relativePath = FileSystem.Path.GetRelativePath(gameRoot, file);

                    // Remove read-only attribute if present
                    var fileInfo = FileSystem.FileInfo.New(file);
                    if (fileInfo.Exists && fileInfo.IsReadOnly)
                        fileInfo.IsReadOnly = false;

                    FileSystem.File.Delete(file);
                    processedFiles.Add(relativePath);
                    Logger.Debug("Removed: {File}", relativePath);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to remove {file}: {ex.Message}";
                    errors.Add(error);
                    Logger.Warning(ex, "Failed to remove file: {File}", file);
                }
            }

            return Task.FromResult(new GameFileOperationResult
            {
                Success = errors.Count == 0,
                Message = errors.Count == 0
                    ? $"Successfully removed {processedFiles.Count} {Category} files."
                    : $"Removed {processedFiles.Count} files with {errors.Count} errors.",
                ProcessedFiles = processedFiles,
                Errors = errors
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during remove operation for category: {Category}", Category);
            return Task.FromResult(new GameFileOperationResult
            {
                Success = false,
                Message = $"Remove operation failed: {ex.Message}"
            });
        }
    }

    protected virtual List<string> GetFilesForCategory(string gameRoot)
    {
        var files = new List<string>();

        foreach (var pattern in FilePatterns)
        {
            try
            {
                var matchingFiles = FileSystem.Directory.GetFiles(gameRoot, pattern, SearchOption.TopDirectoryOnly);
                files.AddRange(matchingFiles);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error searching for pattern {Pattern} in {GameRoot}", pattern, gameRoot);
            }
        }

        return files.Distinct().ToList();
    }
}
