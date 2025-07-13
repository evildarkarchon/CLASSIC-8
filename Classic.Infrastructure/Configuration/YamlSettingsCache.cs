using System.Collections.Concurrent;
using Classic.Core.Interfaces;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Classic.Infrastructure.Configuration;

/// <summary>
/// Legacy YAML settings cache implementation.
/// </summary>
/// <remarks>
/// This class is deprecated. Use ISettingsService instead for all settings access.
/// </remarks>
[Obsolete("Use ISettingsService instead. This class will be removed in a future version.")]
#pragma warning disable CS0618 // Type or member is obsolete
public class YamlSettingsCache(ILogger logger) : IYamlSettingsCache
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly ConcurrentDictionary<string, object> _cache = new();

    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public T GetSetting<T>(string file, string path, T defaultValue = default)
    {
        try
        {
            var cacheKey = $"{file}:{path}";
            if (_cache.TryGetValue(cacheKey, out var cachedValue)) return (T)cachedValue;

            if (!File.Exists(file))
            {
                logger.Warning("YAML file not found: {File}", file);
                return defaultValue;
            }

            var yamlContent = File.ReadAllText(file);
            var yamlObject = _deserializer.Deserialize<Dictionary<string, object>>(yamlContent);

            var value = GetNestedValue(yamlObject, path, defaultValue);
            if (value != null) _cache.TryAdd(cacheKey, value);

            return value;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reading YAML setting from {File} at path {Path}", file, path);
            return defaultValue;
        }
    }

    public async Task<T> GetSettingAsync<T>(string file, string path, T defaultValue = default)
    {
        try
        {
            var cacheKey = $"{file}:{path}";
            if (_cache.TryGetValue(cacheKey, out var cachedValue)) return (T)cachedValue;

            if (!File.Exists(file))
            {
                logger.Warning("YAML file not found: {File}", file);
                return defaultValue;
            }

            var yamlContent = await File.ReadAllTextAsync(file);
            var yamlObject = _deserializer.Deserialize<Dictionary<string, object>>(yamlContent);

            var value = GetNestedValue(yamlObject, path, defaultValue);
            if (value != null) _cache.TryAdd(cacheKey, value);

            return value;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error reading YAML setting from {File} at path {Path}", file, path);
            return defaultValue;
        }
    }

    public void ReloadCache()
    {
        _cache.Clear();
        logger.Information("YAML settings cache cleared");
    }

    private T GetNestedValue<T>(Dictionary<string, object> yamlObject, string path, T defaultValue)
    {
        var keys = path.Split('.');
        object current = yamlObject;

        foreach (var key in keys)
            if (current is Dictionary<string, object> dict && dict.TryGetValue(key, out var value))
                current = value;
            else
                return defaultValue;

        return current is T result ? result : defaultValue;
    }
}
