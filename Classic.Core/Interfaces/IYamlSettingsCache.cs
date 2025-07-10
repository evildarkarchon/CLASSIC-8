namespace Classic.Core.Interfaces;

public interface IYamlSettingsCache
{
    T GetSetting<T>(string file, string path, T defaultValue = default);
    Task<T> GetSettingAsync<T>(string file, string path, T defaultValue = default);
    void ReloadCache();
}
