namespace Classic.Core.Interfaces;

/// <summary>
/// Provides game file management operations including backup, restore, and remove functionality.
/// </summary>
public interface IGameFileManager
{
    /// <summary>
    /// Backs up game files for the specified category.
    /// </summary>
    /// <param name="category">The category of files to backup (XSE, RESHADE, VULKAN, ENB)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task<GameFileOperationResult> BackupFilesAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores game files for the specified category.
    /// </summary>
    /// <param name="category">The category of files to restore (XSE, RESHADE, VULKAN, ENB)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task<GameFileOperationResult> RestoreFilesAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes game files for the specified category.
    /// </summary>
    /// <param name="category">The category of files to remove (XSE, RESHADE, VULKAN, ENB)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task<GameFileOperationResult> RemoveFilesAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a backup exists for the specified category.
    /// </summary>
    /// <param name="category">The category to check</param>
    /// <returns>True if backup exists, false otherwise</returns>
    bool HasBackup(string category);

    /// <summary>
    /// Gets the backup directory for the specified category.
    /// </summary>
    /// <param name="category">The category</param>
    /// <returns>The backup directory path</returns>
    string GetBackupDirectory(string category);
}

/// <summary>
/// Represents the result of a game file operation.
/// </summary>
public class GameFileOperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<string> ProcessedFiles { get; init; } = [];
    public List<string> Errors { get; init; } = [];
}
