namespace CLASSIC_8.Core.Yaml;

public interface IYamlSettingsCache
{
    /// <summary>
    ///     Gets the file path for a given YAML configuration type.
    /// </summary>
    /// <param name="yamlStore">The identifier for the configuration type.</param>
    /// <returns>The resolved file path corresponding to the provided YAML store.</returns>
    string GetPathForStore(YamlStore yamlStore);

    /// <summary>
    ///     Retrieves or updates a setting from a nested YAML data structure.
    /// </summary>
    /// <typeparam name="T">The expected type of the setting value.</typeparam>
    /// <param name="yamlStore">The YAML store from which the setting is retrieved or updated.</param>
    /// <param name="keyPath">The dot-delimited path specifying the location of the setting within the YAML structure.</param>
    /// <param name="newValue">The new value to update the setting with. If null, the method operates as a read.</param>
    /// <returns>The existing or updated setting value if successful, otherwise default(T).</returns>
    T? GetSetting<T>(YamlStore yamlStore, string keyPath, T? newValue = default);

    /// <summary>
    ///     Forces a reload of the YAML file, ignoring the cache.
    /// </summary>
    /// <param name="yamlStore">The YAML store to reload.</param>
    void ReloadYamlFile(YamlStore yamlStore);

    /// <summary>
    ///     Clears all caches. Useful for testing scenarios.
    /// </summary>
    void ClearAllCaches();
}