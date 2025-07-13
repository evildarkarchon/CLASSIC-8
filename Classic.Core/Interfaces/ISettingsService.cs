using Classic.Core.Models.Settings;

namespace Classic.Core.Interfaces;

/// <summary>
/// Provides strongly-typed access to application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the main application settings.
    /// </summary>
    ClassicSettings Settings { get; }

    /// <summary>
    /// Saves any pending changes to settings.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Reloads settings from disk.
    /// </summary>
    void Reload();

    /// <summary>
    /// Gets a specific setting value.
    /// </summary>
    T? GetSetting<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Sets a specific setting value.
    /// </summary>
    void SetSetting<T>(string key, T value);
}
