using System.Reflection;
using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Classic.Core.Models.Settings;
using Serilog;

namespace Classic.Infrastructure.Configuration;

/// <summary>
/// Provides comprehensive settings management with automatic persistence and support for multiple YAML stores.
/// This is the unified entry point for all settings access in the application.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly IYamlSettingsProvider _yamlProvider;
    private readonly ILogger _logger;
    private ClassicSettings? _cachedSettings;
    private readonly object _lock = new();

    public SettingsService(IYamlSettingsProvider yamlProvider, ILogger logger)
    {
        _yamlProvider = yamlProvider;
        _logger = logger;
    }

    public ClassicSettings Settings
    {
        get
        {
            lock (_lock)
            {
                if (_cachedSettings == null) LoadSettings();
                return _cachedSettings!;
            }
        }
    }

    public async Task SaveAsync()
    {
        lock (_lock)
        {
            if (_cachedSettings != null) SaveSettingsToYaml(_cachedSettings);
        }

        await _yamlProvider.SaveAsync();
        _logger.Information("Settings saved successfully");
    }

    public void Reload()
    {
        lock (_lock)
        {
            _yamlProvider.Reload();
            _cachedSettings = null;
            LoadSettings();
        }

        _logger.Information("Settings reloaded from disk");
    }

    // Main settings store convenience methods
    public T? GetSetting<T>(string key, T? defaultValue = default)
    {
        return _yamlProvider.Get<T>(YamlStore.Settings, $"CLASSIC_Settings.{key}", defaultValue);
    }

    public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
    {
        return await _yamlProvider.GetAsync<T>(YamlStore.Settings, $"CLASSIC_Settings.{key}", defaultValue);
    }

    public void SetSetting<T>(string key, T value)
    {
        _yamlProvider.Set(YamlStore.Settings, $"CLASSIC_Settings.{key}", value);

        // Update cached settings if the property exists
        lock (_lock)
        {
            if (_cachedSettings != null)
            {
                var property = typeof(ClassicSettings).GetProperty(key.Replace(" ", ""));
                if (property != null && property.CanWrite) property.SetValue(_cachedSettings, value);
            }
        }
    }

    public async Task SetSettingAsync<T>(string key, T value)
    {
        await _yamlProvider.SetAsync(YamlStore.Settings, $"CLASSIC_Settings.{key}", value);

        // Update cached settings if the property exists
        lock (_lock)
        {
            if (_cachedSettings != null)
            {
                var property = typeof(ClassicSettings).GetProperty(key.Replace(" ", ""));
                if (property != null && property.CanWrite) property.SetValue(_cachedSettings, value);
            }
        }
    }

    // Direct YAML store access methods
    public T? GetSetting<T>(YamlStore store, string path, T? defaultValue = default)
    {
        return _yamlProvider.Get<T>(store, path, defaultValue);
    }

    public async Task<T?> GetSettingAsync<T>(YamlStore store, string path, T? defaultValue = default)
    {
        return await _yamlProvider.GetAsync<T>(store, path, defaultValue);
    }

    public void SetSetting<T>(YamlStore store, string path, T value)
    {
        _yamlProvider.Set<T>(store, path, value);
    }

    public async Task SetSettingAsync<T>(YamlStore store, string path, T value)
    {
        await _yamlProvider.SetAsync<T>(store, path, value);
    }

    public bool SettingExists(YamlStore store, string path)
    {
        return _yamlProvider.Exists(store, path);
    }

    public string GetStorePath(YamlStore store)
    {
        return _yamlProvider.GetStorePath(store);
    }

    private void LoadSettings()
    {
        _cachedSettings = new ClassicSettings();

        // Load each property from YAML
        foreach (var property in typeof(ClassicSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite) continue;

            var yamlKey = ConvertPropertyNameToYamlKey(property.Name);
            var defaultValue = property.GetValue(_cachedSettings);

            try
            {
                var value = GetSettingForProperty(property, yamlKey, defaultValue);
                if (value != null) property.SetValue(_cachedSettings, value);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to load setting {Key}, using default value", yamlKey);
            }
        }

        _logger.Debug("Loaded settings from YAML");
    }

    private object? GetSettingForProperty(PropertyInfo property, string yamlKey, object? defaultValue)
    {
        var method = typeof(IYamlSettingsProvider).GetMethod(nameof(IYamlSettingsProvider.Get))!;
        var genericMethod = method.MakeGenericMethod(property.PropertyType);

        return genericMethod.Invoke(_yamlProvider, [YamlStore.Settings, $"CLASSIC_Settings.{yamlKey}", defaultValue]);
    }

    private void SaveSettingsToYaml(ClassicSettings settings)
    {
        foreach (var property in typeof(ClassicSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead) continue;

            var yamlKey = ConvertPropertyNameToYamlKey(property.Name);
            var value = property.GetValue(settings);

            if (value != null)
            {
                var method = typeof(IYamlSettingsProvider).GetMethod(nameof(IYamlSettingsProvider.Set))!;
                var genericMethod = method.MakeGenericMethod(property.PropertyType);
                _yamlProvider.Set(YamlStore.Settings, $"CLASSIC_Settings.{yamlKey}", value);
            }
        }
    }

    private static string ConvertPropertyNameToYamlKey(string propertyName)
    {
        // Convert from PascalCase to space-separated words
        // e.g., "ManagedGame" -> "Managed Game", "FCXMode" -> "FCX Mode"
        var result = propertyName[0].ToString();

        for (var i = 1; i < propertyName.Length; i++)
        {
            if (char.IsUpper(propertyName[i]))
            {
                // Check if it's an acronym (consecutive capitals)
                if (i + 1 < propertyName.Length && !char.IsUpper(propertyName[i + 1]))
                    result += " ";
                else if (i > 0 && !char.IsUpper(propertyName[i - 1])) result += " ";
            }

            result += propertyName[i];
        }

        return result;
    }
}
