using Classic.Core.Models;

namespace Classic.Core.Interfaces;

/// <summary>
/// Service for playing audio notifications with cross-platform support
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Plays an embedded audio resource with specified volume
    /// </summary>
    /// <param name="resourceName">Name of the embedded resource</param>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    /// <returns>Task representing the async operation</returns>
    Task PlayEmbeddedResourceAsync(string resourceName, double volume = 0.5);

    /// <summary>
    /// Plays a notification sound based on the notification type
    /// </summary>
    /// <param name="type">Type of notification to play</param>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    /// <returns>Task representing the async operation</returns>
    Task PlayNotificationAsync(NotificationType type, double volume = 0.5);

    /// <summary>
    /// Checks if audio playback is available on the current platform
    /// </summary>
    /// <returns>True if audio playback is supported</returns>
    bool IsAudioSupported();
}
