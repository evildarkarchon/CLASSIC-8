namespace Classic.Core.Models;

/// <summary>
/// Types of notifications
/// </summary>
public enum NotificationType
{
    Information,
    Success,
    Warning,
    Error
}

/// <summary>
/// Notification message
/// </summary>
public class NotificationMessage
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Information;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(5);
    public bool IsActionable { get; set; }
    public string? ActionText { get; set; }
    public Action? ActionCallback { get; set; }
}

/// <summary>
/// Progress update event arguments
/// </summary>
public class ProgressUpdateEventArgs : EventArgs
{
    public ProgressState State { get; set; } = new();
}

/// <summary>
/// Current progress state
/// </summary>
public class ProgressState
{
    public string OperationName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int CurrentItem { get; set; }
    public int Percentage { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public string? Details { get; set; }
    public bool IsIndeterminate { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCompleted { get; set; }
    public bool HasError { get; set; }
}

/// <summary>
/// Toast notification configuration
/// </summary>
public class ToastConfiguration
{
    public bool ShowToasts { get; set; } = true;
    public int DefaultDurationSeconds { get; set; } = 5;
    public int MaxConcurrentToasts { get; set; } = 3;
    public bool PlaySounds { get; set; } = true;
    public bool ShowInTaskbar { get; set; } = true;
}
