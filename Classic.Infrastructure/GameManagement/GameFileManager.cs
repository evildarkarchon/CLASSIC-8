using System.IO.Abstractions;
using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Serilog;

namespace Classic.Infrastructure.GameManagement;

/// <summary>
/// Manages game file operations including backup, restore, and removal using strategy pattern.
/// </summary>
public class GameFileManager : IGameFileManager
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IFileOperationStrategyFactory _strategyFactory;

    // ReSharper disable once NotAccessedField.Local - Reserved for future game detection implementation
    private readonly ISettingsService _settingsService;

    public GameFileManager(
        IFileSystem fileSystem,
        ILogger logger,
        ISettingsService settingsService,
        IFileOperationStrategyFactory strategyFactory)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _settingsService = settingsService;
        _strategyFactory = strategyFactory;
    }

    public async Task<GameFileOperationResult> BackupFilesAsync(string category,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteOperationAsync(GameFileOperation.Backup, category, cancellationToken).ConfigureAwait(false);
    }

    public async Task<GameFileOperationResult> RestoreFilesAsync(string category,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteOperationAsync(GameFileOperation.Restore, category, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GameFileOperationResult> RemoveFilesAsync(string category,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteOperationAsync(GameFileOperation.Remove, category, cancellationToken).ConfigureAwait(false);
    }

    private async Task<GameFileOperationResult> ExecuteOperationAsync(
        GameFileOperation operation,
        string category,
        CancellationToken cancellationToken)
    {
        _logger.Information("Starting {Operation} operation for category: {Category}", operation, category);

        try
        {
            var strategy = _strategyFactory.GetStrategy(category);
            if (strategy == null)
            {
                return new GameFileOperationResult
                {
                    Success = false,
                    Message =
                        $"Unknown category: {category}. Available categories: {string.Join(", ", _strategyFactory.GetAvailableCategories())}"
                };
            }

            var gameRoot = GetGameRootDirectory();
            if (string.IsNullOrEmpty(gameRoot) || !_fileSystem.Directory.Exists(gameRoot))
            {
                return new GameFileOperationResult
                {
                    Success = false,
                    Message = "Game root directory not found. Please configure game paths in settings."
                };
            }

            var backupDir = GetBackupDirectory(category);
            return await strategy.ExecuteAsync(operation, gameRoot, backupDir, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during {Operation} operation for category: {Category}", operation, category);
            return new GameFileOperationResult
            {
                Success = false,
                Message = $"{operation} operation failed: {ex.Message}"
            };
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
        // For now, return empty - this should be configured via settings
        // In a real implementation, this would come from game detection or user configuration
        return string.Empty;
    }
}
