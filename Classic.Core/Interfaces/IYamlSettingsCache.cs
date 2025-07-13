namespace Classic.Core.Interfaces;

/// <summary>
/// Simple caching interface for YAML settings.
/// </summary>
/// <remarks>
/// This interface is deprecated. Use ISettingsService instead for all settings access.
/// ISettingsService provides better performance, caching, and a more comprehensive API.
/// </remarks>
[Obsolete("Use ISettingsService instead. This interface will be removed in a future version.")]
public interface IYamlSettingsCache
{
    T GetSetting<T>(string file, string path, T defaultValue = default);
    Task<T> GetSettingAsync<T>(string file, string path, T defaultValue = default);
    void ReloadCache();
}
