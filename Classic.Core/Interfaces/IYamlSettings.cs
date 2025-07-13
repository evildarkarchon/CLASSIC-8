using Classic.Core.Enums;

namespace Classic.Core.Interfaces;

/// <summary>
/// Provides a high-level interface for accessing and modifying YAML settings.
/// </summary>
public interface IYamlSettings
{
    /// <summary>
    /// Gets a setting value from the specified YAML store.
    /// </summary>
    T? Get<T>(YamlStore store, string path, T? defaultValue = default);

    /// <summary>
    /// Gets a setting value asynchronously from the specified YAML store.
    /// </summary>
    Task<T?> GetAsync<T>(YamlStore store, string path, T? defaultValue = default);

    /// <summary>
    /// Sets a setting value in the specified YAML store.
    /// </summary>
    void Set<T>(YamlStore store, string path, T value);

    /// <summary>
    /// Sets a setting value asynchronously in the specified YAML store.
    /// </summary>
    Task SetAsync<T>(YamlStore store, string path, T value);

    /// <summary>
    /// Checks if a setting exists in the specified YAML store.
    /// </summary>
    bool Exists(YamlStore store, string path);

    /// <summary>
    /// Saves all pending changes to disk.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Reloads settings from disk, discarding any unsaved changes.
    /// </summary>
    void Reload();

    /// <summary>
    /// Gets the file path for a specific YAML store.
    /// </summary>
    string GetStorePath(YamlStore store);
}
