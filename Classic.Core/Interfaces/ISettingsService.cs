using Classic.Core.Enums;
using Classic.Core.Models.Settings;

namespace Classic.Core.Interfaces;

/// <summary>
/// Provides comprehensive access to application settings with support for multiple stores and async operations.
/// This is the primary interface for all settings access in the application.
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
    /// Gets a specific setting value from the main settings store.
    /// </summary>
    T? GetSetting<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Gets a setting value asynchronously from the main settings store.
    /// </summary>
    Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Sets a specific setting value in the main settings store.
    /// </summary>
    void SetSetting<T>(string key, T value);

    /// <summary>
    /// Sets a setting value asynchronously in the main settings store.
    /// </summary>
    Task SetSettingAsync<T>(string key, T value);

    /// <summary>
    /// Gets a setting value from a specific YAML store.
    /// </summary>
    T? GetSetting<T>(YamlStore store, string path, T? defaultValue = default);

    /// <summary>
    /// Gets a setting value asynchronously from a specific YAML store.
    /// </summary>
    Task<T?> GetSettingAsync<T>(YamlStore store, string path, T? defaultValue = default);

    /// <summary>
    /// Sets a setting value in a specific YAML store.
    /// </summary>
    void SetSetting<T>(YamlStore store, string path, T value);

    /// <summary>
    /// Sets a setting value asynchronously in a specific YAML store.
    /// </summary>
    Task SetSettingAsync<T>(YamlStore store, string path, T value);

    /// <summary>
    /// Checks if a setting exists in a specific YAML store.
    /// </summary>
    bool SettingExists(YamlStore store, string path);

    /// <summary>
    /// Gets the file path for a specific YAML store.
    /// </summary>
    string GetStorePath(YamlStore store);
}
