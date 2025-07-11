using Classic.Core.Interfaces;
using Classic.Core.Models;
using Serilog;
using System.Collections.Concurrent;
using Classic.Infrastructure.Platform;

namespace Classic.Infrastructure.Services;

/// <summary>
/// Service for displaying notifications to the user
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger _logger;
    private readonly ISettingsService _settingsService;
    private readonly ConcurrentQueue<NotificationMessage> _notifications = new();
    private readonly Timer _cleanupTimer;

    public event EventHandler<NotificationMessage>? NotificationAdded;

    public NotificationService(ILogger logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
        
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
            await PlayAudioNotificationAsync(type);
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
            // Try platform-specific audio first, then fallback to generic beep
            if (OperatingSystem.IsWindows())
            {
                await PlayWindowsSystemSoundAsync(type);
            }
            else if (OperatingSystem.IsLinux())
            {
                await PlayLinuxSystemSoundAsync(type);
            }
            else if (OperatingSystem.IsMacOS())
            {
                await PlayMacSystemSoundAsync(type);
            }
            else
            {
                // Fallback for other platforms
                await PlayGenericSoundAsync(type);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to play audio notification for type {Type}", type);
            // Try one final fallback
            await PlayGenericSoundAsync(type);
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

    private async Task PlayWindowsSystemSoundAsync(NotificationType type)
    {
        // Use system sounds on Windows
        await Task.Run(() =>
        {
            try
            {
                // Try to play Windows system sound using rundll32
                var soundName = type switch
                {
                    NotificationType.Error => "SystemHand",
                    NotificationType.Warning => "SystemExclamation", 
                    NotificationType.Success => "SystemAsterisk",
                    NotificationType.Information => "SystemDefault",
                    _ => "SystemDefault"
                };

                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "rundll32";
                process.StartInfo.Arguments = $"user32.dll,MessageBeep 0x00000040"; // MB_ICONINFORMATION
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                
                process.Start();
                process.WaitForExit(1000); // Wait max 1 second
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to play Windows system sound, trying fallback");
                // Fallback to console beep using cross-platform helper
                CrossPlatformHelper.TryConsoleBeep();
            }
        });
    }

    private async Task PlayLinuxSystemSoundAsync(NotificationType type)
    {
        // Use paplay or aplay on Linux
        await Task.Run(async () =>
        {
            // Try different Linux audio methods in order of preference
            var audioMethods = new[]
            {
                ("paplay", "/usr/share/sounds/alsa/Front_Left.wav"),
                ("aplay", "/usr/share/sounds/alsa/Front_Left.wav"),
                ("paplay", "/usr/share/sounds/freedesktop/stereo/bell.oga"),
                ("speaker-test", "-t sine -f 1000 -l 1"),
                ("echo", "\\a") // Bell character
            };

            foreach (var (command, args) in audioMethods)
            {
                try
                {
                    var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = command;
                    process.StartInfo.Arguments = args;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    
                    process.Start();
                    var timeout = Task.Delay(2000); // 2 second timeout
                    var processTask = process.WaitForExitAsync();
                    
                    var completedTask = await Task.WhenAny(processTask, timeout);
                    if (completedTask == processTask && process.ExitCode == 0)
                    {
                        return; // Success
                    }
                    
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Failed to play Linux system sound using {Command}", command);
                }
            }
            
            _logger.Warning("All Linux audio methods failed");
        });
    }

    private async Task PlayMacSystemSoundAsync(NotificationType type)
    {
        // Use afplay on macOS
        await Task.Run(async () =>
        {
            try
            {
                var soundName = type switch
                {
                    NotificationType.Error => "Basso",
                    NotificationType.Warning => "Sosumi",
                    NotificationType.Success => "Glass", 
                    NotificationType.Information => "Ping",
                    _ => "Ping"
                };

                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "afplay";
                process.StartInfo.Arguments = $"/System/Library/Sounds/{soundName}.aiff";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                
                process.Start();
                var timeout = Task.Delay(3000); // 3 second timeout
                var processTask = process.WaitForExitAsync();
                
                var completedTask = await Task.WhenAny(processTask, timeout);
                if (completedTask == processTask && process.ExitCode == 0)
                {
                    return; // Success
                }
                
                if (!process.HasExited)
                {
                    process.Kill();
                }
                
                // Try alternative method
                process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "osascript";
                process.StartInfo.Arguments = "-e 'beep'";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                
                process.Start();
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to play macOS system sound");
            }
        });
    }
    
    private async Task PlayGenericSoundAsync(NotificationType type)
    {
        // Generic fallback for unsupported platforms
        await Task.Run(() =>
        {
            try
            {
                _logger.Information("Playing generic notification sound for {Type}", type);
                // For unsupported platforms, we just log - no audio
                // This prevents exceptions on platforms like FreeBSD, etc.
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to play generic notification sound");
            }
        });
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