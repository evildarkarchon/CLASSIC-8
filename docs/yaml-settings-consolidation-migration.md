# YAML Settings Consolidation Migration Guide

## Overview

The YAML settings system has been consolidated from multiple interfaces into a single, comprehensive `ISettingsService` interface. This guide helps you migrate from the old interfaces to the new consolidated system.

## Changes Made

### New Unified Interface

**`ISettingsService`** is now the single public interface for all settings access:

```csharp
public interface ISettingsService
{
    // Strongly-typed settings access
    ClassicSettings Settings { get; }
    
    // Main settings convenience methods
    T? GetSetting<T>(string key, T? defaultValue = default);
    Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default);
    void SetSetting<T>(string key, T value);
    Task SetSettingAsync<T>(string key, T value);
    
    // Direct YAML store access
    T? GetSetting<T>(YamlStore store, string path, T? defaultValue = default);
    Task<T?> GetSettingAsync<T>(YamlStore store, string path, T? defaultValue = default);
    void SetSetting<T>(YamlStore store, string path, T value);
    Task SetSettingAsync<T>(YamlStore store, string path, T value);
    
    // Utility methods
    bool SettingExists(YamlStore store, string path);
    string GetStorePath(YamlStore store);
    Task SaveAsync();
    void Reload();
}
```

### Deprecated Interfaces

The following interfaces are now **obsolete** and will be removed in a future version:

- `IYamlSettings` - Use `ISettingsService` instead
- `IYamlSettingsCache` - Use `ISettingsService` instead

### Internal Implementation

The internal implementation now uses:
- `IYamlSettingsProvider` (internal interface)
- `YamlSettings` (internal implementation)

## Migration Steps

### 1. Update Constructor Dependencies

**Before:**
```csharp
public MyService(IYamlSettings yamlSettings, ILogger logger)
public MyService(IYamlSettingsCache yamlCache, ILogger logger)
```

**After:**
```csharp
public MyService(ISettingsService settingsService, ILogger logger)
```

### 2. Update Method Calls

**Before (IYamlSettings):**
```csharp
var value = _yamlSettings.Get<string>(YamlStore.Settings, "CLASSIC_Settings.Managed Game", "Fallout 4");
await _yamlSettings.SetAsync(YamlStore.Settings, "CLASSIC_Settings.VR Mode", true);
```

**After:**
```csharp
// For main settings (automatically prefixed)
var value = _settingsService.GetSetting<string>("Managed Game", "Fallout 4");
await _settingsService.SetSettingAsync("VR Mode", true);

// Or for direct store access
var value = _settingsService.GetSetting<string>(YamlStore.Settings, "CLASSIC_Settings.Managed Game", "Fallout 4");
await _settingsService.SetSettingAsync(YamlStore.Settings, "CLASSIC_Settings.VR Mode", true);
```

**Before (IYamlSettingsCache):**
```csharp
var value = await _yamlCache.GetSettingAsync<string>("settings.yaml", "Classic.Managed Game");
```

**After:**
```csharp
var value = await _settingsService.GetSettingAsync<string>("Managed Game");
```

### 3. Access Strongly-Typed Settings

**New Capability:**
```csharp
// Access the full settings object
var settings = _settingsService.Settings;
var managedGame = settings.ManagedGame;
var vrMode = settings.VRMode;

// Settings are automatically synchronized when using individual methods
await _settingsService.SetSettingAsync("VR Mode", true);
// settings.VRMode is now automatically updated
```

## Benefits of the New System

1. **Single Interface**: No confusion about which service to use
2. **Strongly-Typed Access**: Direct access to `ClassicSettings` object
3. **Better Performance**: Unified caching and optimized operations
4. **Comprehensive API**: All YAML operations in one place
5. **Automatic Synchronization**: Changes sync between individual settings and the strongly-typed object
6. **Better Error Handling**: Consistent error handling across all operations

## Backward Compatibility

- Old interfaces are marked as `[Obsolete]` but still work
- Legacy services are still registered for transition period
- Existing code will compile with warnings until migrated

## Examples

### Complete Migration Example

**Before:**
```csharp
public class GameConfiguration : IGameConfiguration
{
    private readonly IYamlSettingsCache _yamlSettings;
    
    public GameConfiguration(IYamlSettingsCache yamlSettings, ILogger logger)
    {
        _yamlSettings = yamlSettings;
        // ... 
    }
    
    private async Task InitializeAsync()
    {
        _isVrMode = await _yamlSettings.GetSettingAsync<bool?>("Classic", "VR Mode") ?? false;
        _currentGame = await _yamlSettings.GetSettingAsync<string>("Classic", "Managed Game") ?? "Fallout4";
    }
}
```

**After:**
```csharp
public class GameConfiguration : IGameConfiguration
{
    private readonly ISettingsService _settingsService;
    
    public GameConfiguration(ISettingsService settingsService, ILogger logger)
    {
        _settingsService = settingsService;
        // ... 
    }
    
    private async Task InitializeAsync()
    {
        _isVrMode = await _settingsService.GetSettingAsync<bool?>("VR Mode") ?? false;
        _currentGame = await _settingsService.GetSettingAsync<string>("Managed Game") ?? "Fallout4";
        
        // Or even better, use strongly-typed access:
        var settings = _settingsService.Settings;
        _isVrMode = settings.VRMode;
        _currentGame = settings.ManagedGame;
    }
}
```

## Need Help?

If you encounter issues during migration:

1. Check that you've updated constructor dependencies to use `ISettingsService`
2. Verify that setting key names match (remove "CLASSIC_Settings." prefix for convenience methods)
3. Consider using strongly-typed `Settings` property for better performance
4. Ensure `await` is used for async methods

For complex migrations or questions, consult the development team.
