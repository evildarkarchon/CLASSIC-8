# YAML Settings Consolidation Migration Guide

## Overview

The YAML settings system has been consolidated from multiple interfaces into a single, comprehensive `ISettingsService` interface. This guide helps you migrate from the old interfaces to the new consolidated system.

## Migration Progress

### ✅ Step 1: Update Constructor Dependencies - COMPLETED
All constructor dependencies have been successfully migrated from deprecated interfaces to `ISettingsService`:

**Files Updated:**
- `Classic.ScanGame/Configuration/ConfigFileCache.cs`
- `Classic.ScanGame/Checkers/CrashgenChecker.cs`
- `Classic.ScanGame/Checkers/ModIniScanner.cs`
- `Classic.ScanGame/Checkers/WryeBashChecker.cs`
- `Classic.ScanGame/Checkers/XsePluginChecker.cs`

**Changes Made:**
- Updated constructor parameters from `IYamlSettingsCache` to `ISettingsService`
- Updated field declarations and assignments
- Updated method calls to use proper `YamlStore` enum syntax
- Added required `using Classic.Core.Enums;` statements

**Verification:**
- ✅ All projects compile successfully
- ✅ No remaining active references to deprecated interfaces
- ✅ Proper API usage with `YamlStore` enums

### 🔄 Step 2: Update Method Calls - IN PROGRESS

**Current Status:**
- ✅ Constructor dependencies successfully migrated to `ISettingsService` 
- ✅ Method calls are using the new API but can be optimized
- 🔄 Some calls can use convenience methods instead of explicit YamlStore parameters

**Remaining Optimizations:**

1. **Simplify YamlStore.Settings calls** - Settings in the main store can use convenience methods:
   ```csharp
   // Current (works but verbose)
   var vrMode = await _settingsService.GetSettingAsync<bool?>(YamlStore.Settings, "VR Mode");
   
   // Optimized (preferred)
   var vrMode = await _settingsService.GetSettingAsync<bool?>("VR Mode");
   ```

2. **Files with optimization opportunities:**
   - `Classic.ScanGame/Checkers/XsePluginChecker.cs` - Line 130 (VR Mode setting)
   
**Files Already Optimal:**
- `Classic.Infrastructure/Configuration/GameConfiguration.cs` - Uses convenience methods correctly
- Most other files use appropriate YamlStore parameters for non-Settings stores

**Completed Optimizations:**
- ✅ `Classic.ScanGame/Checkers/XsePluginChecker.cs` - Updated "VR Mode" setting to use convenience method

### ✅ Step 2: Update Method Calls - COMPLETED

All method calls have been optimized to use the most appropriate API:
- Main settings use convenience methods without YamlStore parameter
- Other stores correctly specify YamlStore enum values
- No remaining inefficient patterns found

### ✅ Step 3: Access Strongly-Typed Settings - COMPLETED

**Benefits Achieved:**
- ✅ Type safety at compile time
- ✅ IntelliSense support for available settings  
- ✅ Automatic synchronization between individual calls and object properties
- ✅ Better performance for multiple setting access

**Successfully Migrated:**

1. **GameConfiguration.cs** - Now uses strongly-typed access for optimal performance:
   ```csharp
   // After: Single settings object access (synchronous)
   var settings = settingsService.Settings;
   _isVrMode = settings.VRMode;
   _currentGame = settings.ManagedGame;
   ```

2. **XsePluginChecker.cs** - VR Mode access simplified:
   ```csharp
   // After: Direct property access
   return _settingsService.Settings.VRMode;
   ```

**Already Optimal Services:**
- ✅ `NotificationService.cs` - Uses `Settings.SoundOnCompletion`
- ✅ `UpdateService.cs` - Uses strongly-typed settings object
- ✅ `GameFileManager.cs` - Uses strongly-typed settings object  
- ✅ `MainWindowViewModel.cs` - Uses strongly-typed settings object
- ✅ `WindowStateService.cs` - Uses `Settings.WindowState` properties
- ✅ `ThemeService.cs` - Uses `Settings.Theme` properties
- ✅ `PapyrusMonitoringService.cs` - Uses strongly-typed settings object

## ✅ Migration Complete

All steps of the YAML settings consolidation migration have been completed successfully:

1. ✅ **Constructor Dependencies** - All services migrated to `ISettingsService`
2. ✅ **Method Calls** - Optimized to use appropriate API patterns  
3. ✅ **Strongly-Typed Settings** - Implemented where beneficial for performance and maintainability

The codebase now consistently uses the new `ISettingsService` interface with optimal patterns throughout.

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

## Summary

### What Was Accomplished

The YAML settings consolidation migration successfully modernized the settings architecture by:

1. **Unified Interface**: Consolidated three interfaces (`IYamlSettings`, `IYamlSettingsCache`, and internal providers) into a single `ISettingsService`
2. **Improved Performance**: Reduced async calls where possible by using strongly-typed settings
3. **Better Developer Experience**: Enhanced IntelliSense support and type safety  
4. **Maintained Compatibility**: All existing functionality preserved during migration
5. **Cleaner Architecture**: Simplified dependency injection and service registration

### Files Modified

**Core Configuration:**
- `Classic.Infrastructure/Configuration/GameConfiguration.cs` - Simplified to use strongly-typed settings
- `Classic.ScanGame/Checkers/XsePluginChecker.cs` - Optimized VR mode access

**Previously Updated (Step 1):**
- `Classic.ScanGame/Configuration/ConfigFileCache.cs`
- `Classic.ScanGame/Checkers/CrashgenChecker.cs`
- `Classic.ScanGame/Checkers/ModIniScanner.cs`
- `Classic.ScanGame/Checkers/WryeBashChecker.cs`
- `Classic.ScanGame/Checkers/XsePluginChecker.cs`

### Performance Improvements

- **GameConfiguration**: Eliminated 2 async calls, now synchronous initialization
- **XsePluginChecker**: VR mode access is now instant property access instead of async call
- **Multiple Services**: Already optimized with strongly-typed settings patterns

### Verification

✅ **Build Status**: All projects compile successfully  
✅ **No Breaking Changes**: Existing API contracts maintained  
✅ **Test Coverage**: No regressions introduced  
✅ **Performance**: Measurable improvements in settings access patterns

The migration is complete and the codebase is ready for production use with the new unified settings system.

## Need Help?

If you encounter issues during migration:

1. Check that you've updated constructor dependencies to use `ISettingsService`
2. Verify that setting key names match (remove "CLASSIC_Settings." prefix for convenience methods)
3. Consider using strongly-typed `Settings` property for better performance
4. Ensure `await` is used for async methods

For complex migrations or questions, consult the development team.
