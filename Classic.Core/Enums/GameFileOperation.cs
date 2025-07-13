namespace Classic.Core.Enums;

/// <summary>
/// Represents the type of game file operation to perform.
/// </summary>
public enum GameFileOperation
{
    /// <summary>
    /// Backup files from game directory to backup location.
    /// </summary>
    Backup,

    /// <summary>
    /// Restore files from backup location to game directory.
    /// </summary>
    Restore,

    /// <summary>
    /// Remove files from game directory.
    /// </summary>
    Remove
}
