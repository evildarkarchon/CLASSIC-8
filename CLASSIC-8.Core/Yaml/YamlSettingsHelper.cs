namespace CLASSIC_8.Core.Yaml;

/// <summary>
///     Provides helper methods for accessing YAML settings with a fluent API.
/// </summary>
public static class YamlSettingsHelper
{
    /// <summary>
    ///     Gets the YAML settings cache instance.
    /// </summary>
    private static IYamlSettingsCache YamlCache => YamlSettingsCache.Instance;

    /// <summary>
    ///     Gets or sets a YAML setting value.
    /// </summary>
    /// <typeparam name="T">The expected type of the setting value.</typeparam>
    /// <param name="yamlStore">The YAML store from which the setting is retrieved or updated.</param>
    /// <param name="keyPath">The dot-delimited path specifying the location of the setting.</param>
    /// <param name="newValue">The new value to update the setting with. If null, reads the current value.</param>
    /// <returns>The existing or updated setting value if successful, otherwise default(T).</returns>
    public static T? YamlSettings<T>(YamlStore yamlStore, string keyPath, T? newValue = default)
    {
        var result = YamlCache.GetSetting<T>(yamlStore, keyPath, newValue);

        // Special handling for Path types
        if (typeof(T) == typeof(FileInfo) && result is string filePath) return (T)(object)new FileInfo(filePath);

        if (typeof(T) == typeof(DirectoryInfo) && result is string dirPath)
            return (T)(object)new DirectoryInfo(dirPath);

        return result;
    }

    /// <summary>
    ///     Fetches a specific setting from the CLASSIC Settings.yaml file.
    ///     Creates the settings file if it does not exist.
    /// </summary>
    /// <typeparam name="T">The expected type of the setting value.</typeparam>
    /// <param name="setting">The key of the setting to retrieve.</param>
    /// <returns>The value of the requested setting, or default(T) if not found.</returns>
    public static T? ClassicSettings<T>(string setting)
    {
        var settingsPath = new FileInfo("CLASSIC Settings.yaml");

        if (!settingsPath.Exists)
        {
            // Get default settings template from Main.yaml
            var defaultSettings = YamlSettings<string>(YamlStore.Main, "CLASSIC_Info.default_settings");

            if (string.IsNullOrEmpty(defaultSettings))
                throw new InvalidOperationException("Invalid Default Settings in 'CLASSIC Main.yaml'");

            File.WriteAllText(settingsPath.FullName, defaultSettings);
        }

        return YamlSettings<T>(YamlStore.Settings, $"CLASSIC_Settings.{setting}");
    }
}