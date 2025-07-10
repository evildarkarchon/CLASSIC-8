using Classic.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Classic.Infrastructure.Configuration;

public class YamlSettingsCache : IYamlSettingsCache
{
    private readonly ILogger<YamlSettingsCache> _logger;
    private readonly ConcurrentDictionary<string, object> _cache;
    private readonly IDeserializer _deserializer;

    public YamlSettingsCache(ILogger<YamlSettingsCache> logger)
    {
        _logger = logger;
        _cache = new ConcurrentDictionary<string, object>();
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    public T GetSetting<T>(string file, string path, T defaultValue = default)
    {
        try
        {
            var cacheKey = $"{file}:{path}";
            if (_cache.TryGetValue(cacheKey, out var cachedValue))
            {
                return (T)cachedValue;
            }

            if (!File.Exists(file))
            {
                _logger.LogWarning("YAML file not found: {File}", file);
                return defaultValue;
            }

            var yamlContent = File.ReadAllText(file);
            var yamlObject = _deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
            
            var value = GetNestedValue(yamlObject, path, defaultValue);
            _cache.TryAdd(cacheKey, value);
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading YAML setting from {File} at path {Path}", file, path);
            return defaultValue;
        }
    }

    public async Task<T> GetSettingAsync<T>(string file, string path, T defaultValue = default)
    {
        try
        {
            var cacheKey = $"{file}:{path}";
            if (_cache.TryGetValue(cacheKey, out var cachedValue))
            {
                return (T)cachedValue;
            }

            if (!File.Exists(file))
            {
                _logger.LogWarning("YAML file not found: {File}", file);
                return defaultValue;
            }

            var yamlContent = await File.ReadAllTextAsync(file);
            var yamlObject = _deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
            
            var value = GetNestedValue(yamlObject, path, defaultValue);
            _cache.TryAdd(cacheKey, value);
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading YAML setting from {File} at path {Path}", file, path);
            return defaultValue;
        }
    }

    public void ReloadCache()
    {
        _cache.Clear();
        _logger.LogInformation("YAML settings cache cleared");
    }

    private T GetNestedValue<T>(Dictionary<string, object> yamlObject, string path, T defaultValue)
    {
        var keys = path.Split('.');
        object current = yamlObject;

        foreach (var key in keys)
        {
            if (current is Dictionary<string, object> dict && dict.TryGetValue(key, out var value))
            {
                current = value;
            }
            else
            {
                return defaultValue;
            }
        }

        return current is T result ? result : defaultValue;
    }
}
