# YAML Processing Implementation

This directory contains the C# port of the Python YAML processing system from CLASSIC.

## Key Components

### YamlStore Enum

Defines the different types of YAML configuration files:

- `Main`: CLASSIC Data/databases/CLASSIC Main.yaml
- `Settings`: CLASSIC Settings.yaml
- `Ignore`: CLASSIC Ignore.yaml
- `Game`: CLASSIC Data/databases/CLASSIC {game}.yaml
- `GameLocal`: CLASSIC Data/CLASSIC {game} Local.yaml
- `Test`: tests/test_settings.yaml

### IYamlSettingsCache Interface

Defines the contract for YAML settings management with methods for:

- Getting file paths for YAML stores
- Reading/writing settings with type safety
- Reloading YAML files

### YamlSettingsCache Class

Main implementation that provides:

- **Singleton pattern** via dependency injection
- **Intelligent caching** with modification time checking for dynamic files
- **Type-safe access** using generics
- **Nested key access** using dot notation (e.g., "CLASSIC_Settings.Managed Game")
- **Separation of static and dynamic files** - static files (Main, Game) are cached permanently
- **Thread-safe operations** using ConcurrentDictionary
- **Automatic type conversion** including support for FileInfo/DirectoryInfo
- **YamlDotNet** for YAML serialization/deserialization

### YamlSettingsHelper Static Class

Provides convenient static methods:

- `YamlSettings<T>()`: Generic method for reading/writing YAML settings
- `ClassicSettings<T>()`: Specialized method for user settings with automatic file creation

## Usage Examples

```csharp
// Initialize the helper (typically in Program.cs)
var gameManager = new GameManager();
var yamlCache = new YamlSettingsCache(gameManager);
YamlSettingsHelper.Initialize(yamlCache);

// Read a setting
var gameName = YamlSettingsHelper.ClassicSettings<string>("Managed Game");

// Write a setting
YamlSettingsHelper.ClassicSettings<bool>("VR Mode", true);

// Read from a specific YAML store
var version = YamlSettingsHelper.YamlSettings<string>(YamlStore.Main, "CLASSIC_Info.version");

// Work with file paths
var rootFolder = YamlSettingsHelper.YamlSettings<DirectoryInfo>(YamlStore.GameLocal, "Root_Folder_Game");
```

## Key Differences from Python Implementation

1. **Type System**: Uses C# generics instead of Python type hints
2. **Threading**: Uses ConcurrentDictionary for thread safety instead of Python's GIL
3. **Path Types**: Uses FileInfo/DirectoryInfo instead of Python's Path
4. **Serialization**: Uses YamlDotNet instead of ruamel.yaml
5. **DI Pattern**: Designed for dependency injection instead of global singleton

## Testing

Unit tests are provided in `CLASSIC-8.Tests/Core/Yaml/` covering:

- Path resolution for different YAML stores
- Reading/writing various data types
- Nested key access
- Cache behavior
- File creation and modification tracking