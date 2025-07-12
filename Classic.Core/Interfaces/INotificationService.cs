using Classic.Core.Models;

namespace Classic.Core.Interfaces;

/// <summary>
/// Service for displaying notifications to the user
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a toast notification
    /// </summary>
    Task ShowToastAsync(string title, string message, NotificationType type = NotificationType.Information);
    
    /// <summary>
    /// Shows a scan completion notification
    /// </summary>
    Task ShowScanCompletedAsync(ScanResult result);
    
    /// <summary>
    /// Shows a scan error notification
    /// </summary>
    Task ShowScanErrorAsync(string operation, Exception exception);
    
    /// <summary>
    /// Shows a game file operation notification
    /// </summary>
    Task ShowGameFileOperationAsync(string operation, string category, GameFileOperationResult result);
    
    /// <summary>
    /// Plays an audio notification if enabled
    /// </summary>
    Task PlayAudioNotificationAsync(NotificationType type);
    
    /// <summary>
    /// Clears all notifications
    /// </summary>
    void ClearNotifications();
    
    /// <summary>
    /// Shows an update available notification
    /// </summary>
    Task ShowUpdateAvailableAsync(string currentVersion, string latestVersion, string downloadUrl);
    
    /// <summary>
    /// Shows a no update available notification
    /// </summary>
    Task ShowNoUpdateAvailableAsync(string currentVersion, string updateSource);
    
    /// <summary>
    /// Shows an update check error notification
    /// </summary>
    Task ShowUpdateCheckErrorAsync(string errorMessage);
}