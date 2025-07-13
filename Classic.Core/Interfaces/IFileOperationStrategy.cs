using Classic.Core.Enums;

namespace Classic.Core.Interfaces;

/// <summary>
/// Strategy interface for game file operations.
/// Provides file patterns and execution logic for specific file categories.
/// </summary>
public interface IFileOperationStrategy
{
    /// <summary>
    /// The category name (e.g., "XSE", "RESHADE", "VULKAN", "ENB").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// File patterns that match files in this category.
    /// </summary>
    string[] FilePatterns { get; }

    /// <summary>
    /// Executes the specified operation for this file category.
    /// </summary>
    /// <param name="operation">The operation to perform</param>
    /// <param name="gameRoot">The game root directory</param>
    /// <param name="backupDir">The backup directory for this category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<GameFileOperationResult> ExecuteAsync(
        GameFileOperation operation,
        string gameRoot,
        string backupDir,
        CancellationToken cancellationToken = default);
}
