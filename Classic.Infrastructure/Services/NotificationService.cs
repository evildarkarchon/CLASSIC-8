using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;
using System.Collections.Concurrent;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Service for displaying notifications to the user
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger _logger;
    private readonly ISettingsService _settingsService;
    private readonly IAudioService _audioService;
    private readonly ConcurrentQueue<NotificationMessage> _notifications = new();
    private readonly Timer _cleanupTimer;

    public event EventHandler<NotificationMessage>? NotificationAdded;

    public NotificationService(ILogger logger, ISettingsService settingsService, IAudioService audioService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _audioService = audioService;
        
        // Setup cleanup timer to remove old notifications
        _cleanupTimer = new Timer(CleanupNotifications, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task ShowToastAsync(string title, string message, NotificationType type = NotificationType.Information)
    {
        var notification = new NotificationMessage
        {
            Title = title,
            Message = message,
            Type = type,
            Timestamp = DateTime.Now,
            Duration = TimeSpan.FromSeconds(GetDurationForType(type))
        };

        _notifications.Enqueue(notification);
        
        _logger.Information("Toast notification: {Title} - {Message} ({Type})", title, message, type);
        
        // Fire event for UI to handle
        NotificationAdded?.Invoke(this, notification);
        
        // Play audio notification if enabled
        if (_settingsService.Settings.SoundOnCompletion)
        {
            await _audioService.PlayNotificationAsync(type, volume: 0.5);
        }
    }

    public async Task ShowScanCompletedAsync(ScanResult result)
    {
        var type = result.IsSuccessful ? NotificationType.Success : 
                   result.HasWarnings ? NotificationType.Warning : 
                   NotificationType.Error;

        var title = result.IsSuccessful ? "Scan Completed Successfully" : 
                   result.HasWarnings ? "Scan Completed with Warnings" : 
                   "Scan Completed with Errors";

        var message = $"Processed {result.TotalLogs} logs in {result.ProcessingTime:mm\\:ss}\n" +
                     $"Success: {result.SuccessfulScans}, Failed: {result.FailedScans}";

        if (result.ModConflicts.Any())
        {
            message += $"\nTop conflicts: {string.Join(", ", result.ModConflicts.Take(3).Select(m => m.Key))}";
        }

        await ShowToastAsync(title, message, type);
    }

    public async Task ShowScanErrorAsync(string operation, Exception exception)
    {
        var title = $"{operation} Failed";
        var message = $"Error: {exception.Message}";
        
        if (exception.InnerException != null)
        {
            message += $"\nDetails: {exception.InnerException.Message}";
        }

        await ShowToastAsync(title, message, NotificationType.Error);
    }

    public async Task ShowGameFileOperationAsync(string operation, string category, GameFileOperationResult result)
    {
        var type = result.Success ? NotificationType.Success : NotificationType.Error;
        var title = $"{operation} {category}";
        var message = result.Message;

        if (result.ProcessedFiles.Count > 0)
        {
            message += $"\nProcessed {result.ProcessedFiles.Count} files";
        }

        await ShowToastAsync(title, message, type);
    }

    public async Task PlayAudioNotificationAsync(NotificationType type)
    {
        if (!_settingsService.Settings.SoundOnCompletion)
            return;

        try
        {
            await _audioService.PlayNotificationAsync(type, volume: 0.5);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to play audio notification for type {Type}", type);
        }
    }

    public void ClearNotifications()
    {
        while (_notifications.TryDequeue(out _))
        {
            // Empty the queue
        }
        
        _logger.Information("All notifications cleared");
    }

    public async Task ShowUpdateAvailableAsync(string currentVersion, string latestVersion, string downloadUrl)
    {
        var title = "Update Available!";
        var message = $"A new version of CLASSIC is available.\n\n" +
                     $"Current version: {currentVersion}\n" +
                     $"Latest version: {latestVersion}\n\n" +
                     (!string.IsNullOrEmpty(downloadUrl) ? $"Download: {downloadUrl}" : "Check GitHub or Nexus for the latest version");

        await ShowToastAsync(title, message, NotificationType.Information);
    }

    public async Task ShowNoUpdateAvailableAsync(string currentVersion, string updateSource)
    {
        var title = "No Updates Available";
        var message = $"You have the latest version of CLASSIC!\n\n" +
                     $"Current version: {currentVersion}\n" +
                     $"Source checked: {updateSource}";

        await ShowToastAsync(title, message, NotificationType.Success);
    }

    public async Task ShowUpdateCheckErrorAsync(string errorMessage)
    {
        var title = "Update Check Failed";
        var message = $"Unable to check for updates.\n\n" +
                     $"Error: {errorMessage}\n\n" +
                     "Please check your internet connection and try again.";

        await ShowToastAsync(title, message, NotificationType.Warning);
    }

    private int GetDurationForType(NotificationType type)
    {
        return type switch
        {
            NotificationType.Error => 10,
            NotificationType.Warning => 7,
            NotificationType.Success => 5,
            NotificationType.Information => 4,
            _ => 5
        };
    }


    private void CleanupNotifications(object? state)
    {
        var cutoff = DateTime.Now - TimeSpan.FromMinutes(5);
        var tempList = new List<NotificationMessage>();
        
        // Keep only recent notifications
        while (_notifications.TryDequeue(out var notification))
        {
            if (notification.Timestamp > cutoff)
            {
                tempList.Add(notification);
            }
        }
        
        foreach (var notification in tempList)
        {
            _notifications.Enqueue(notification);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}