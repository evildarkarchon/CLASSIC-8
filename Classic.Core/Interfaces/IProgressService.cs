using Classic.Core.Models;

namespace Classic.Core.Interfaces;

/// <summary>
/// Service for reporting progress during long-running operations
/// </summary>
public interface IProgressService
{
    /// <summary>
    /// Event fired when progress updates
    /// </summary>
    event EventHandler<ProgressUpdateEventArgs>? ProgressUpdated;

    /// <summary>
    /// Starts a new progress operation
    /// </summary>
    void StartProgress(string operationName, int totalItems = 0);

    /// <summary>
    /// Updates progress with current status
    /// </summary>
    void UpdateProgress(int currentItem, string currentOperation, string? details = null);

    /// <summary>
    /// Reports progress with a percentage
    /// </summary>
    void ReportProgress(int percentage, string message, string? details = null);

    /// <summary>
    /// Completes the current progress operation
    /// </summary>
    void CompleteProgress(string completionMessage);

    /// <summary>
    /// Fails the current progress operation
    /// </summary>
    void FailProgress(string errorMessage);

    /// <summary>
    /// Gets the current progress state
    /// </summary>
    ProgressState CurrentState { get; }
}
