# YAML Configuration Implementation for C#

## Option 1: Custom YAML Configuration Provider (Recommended)

### Required Package
```bash
dotnet add package YamlDotNet
```

### Implementation

#### 1. Create YAML Configuration Source
```csharp
using Microsoft.Extensions.Configuration;

namespace Classic.Infrastructure.Configuration
{
    public class YamlConfigurationSource : IConfigurationSource
    {
        public string Path { get; set; }
        public bool Optional { get; set; }
        public bool ReloadOnChange { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new YamlConfigurationProvider(this);
        }
    }
}
```

#### 2. Create YAML Configuration Provider
```csharp
using Microsoft.Extensions.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Classic.Infrastructure.Configuration
{
    public class YamlConfigurationProvider : FileConfigurationProvider
    {
        public YamlConfigurationProvider(YamlConfigurationSource source) : base(source) { }

        public override void Load(Stream stream)
        {
            var parser = new YamlStream();
            using var reader = new StreamReader(stream);
            parser.Load(reader);

            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (parser.Documents.Count > 0)
            {
                var rootNode = parser.Documents[0].RootNode;
                ExtractData("", rootNode, data);
            }

            Data = data;
        }

        private void ExtractData(string prefix, YamlNode node, Dictionary<string, string> data)
        {
            switch (node)
            {
                case YamlScalarNode scalar:
                    data[prefix] = scalar.Value;
                    break;
                    
                case YamlMappingNode mapping:
                    foreach (var (key, value) in mapping.Children)
                    {
                        var newPrefix = string.IsNullOrEmpty(prefix) 
                            ? key.ToString() 
                            : $"{prefix}:{key}";
                        ExtractData(newPrefix, value, data);
                    }
                    break;
                    
                case YamlSequenceNode sequence:
                    for (int i = 0; i < sequence.Children.Count; i++)
                    {
                        ExtractData($"{prefix}:{i}", sequence.Children[i], data);
                    }
                    break;
            }
        }
    }
}
```

#### 3. Create Extension Methods
```csharp
namespace Classic.Infrastructure.Configuration
{
    public static class YamlConfigurationExtensions
    {
        public static IConfigurationBuilder AddYamlFile(
            this IConfigurationBuilder builder,
            string path,
            bool optional = false,
            bool reloadOnChange = false)
        {
            return builder.Add(new YamlConfigurationSource
            {
                Path = path,
                Optional = optional,
                ReloadOnChange = reloadOnChange
            });
        }
    }
}
```

## Option 2: Direct YAML Settings Cache (More Similar to Python)

This approach is more similar to the Python implementation and better suited for the CLASSIC application's needs:

### Implementation

```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections.Concurrent;

namespace Classic.Infrastructure.Configuration
{
    public interface IYamlSettingsCache
    {
        T GetSetting<T>(string fileName, string path, T defaultValue = default);
        Task<T> GetSettingAsync<T>(string fileName, string path, T defaultValue = default);
        void ReloadCache();
        void ReloadFile(string fileName);
    }

    public class YamlSettingsCache : IYamlSettingsCache
    {
        private readonly ILogger<YamlSettingsCache> _logger;
        private readonly ConcurrentDictionary<string, YamlDocument> _cache;
        private readonly IDeserializer _deserializer;
        private readonly string _basePath;
        private readonly SemaphoreSlim _loadLock;

        public YamlSettingsCache(ILogger<YamlSettingsCache> logger, string basePath = "CLASSIC Data/databases")
        {
            _logger = logger;
            _basePath = basePath;
            _cache = new ConcurrentDictionary<string, YamlDocument>();
            _loadLock = new SemaphoreSlim(1, 1);
            
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
        }

        public T GetSetting<T>(string fileName, string path, T defaultValue = default)
        {
            try
            {
                var document = GetOrLoadDocument(fileName);
                return NavigateAndExtract<T>(document, path) ?? defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting setting {Path} from {FileName}", path, fileName);
                return defaultValue;
            }
        }

        public async Task<T> GetSettingAsync<T>(string fileName, string path, T defaultValue = default)
        {
            try
            {
                var document = await GetOrLoadDocumentAsync(fileName);
                return NavigateAndExtract<T>(document, path) ?? defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting setting {Path} from {FileName}", path, fileName);
                return defaultValue;
            }
        }

        private YamlDocument GetOrLoadDocument(string fileName)
        {
            return _cache.GetOrAdd(fileName, _ => LoadYamlDocument(fileName));
        }

        private async Task<YamlDocument> GetOrLoadDocumentAsync(string fileName)
        {
            if (_cache.TryGetValue(fileName, out var cached))
                return cached;

            await _loadLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_cache.TryGetValue(fileName, out cached))
                    return cached;

                var document = await LoadYamlDocumentAsync(fileName);
                _cache[fileName] = document;
                return document;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        private YamlDocument LoadYamlDocument(string fileName)
        {
            var fullPath = Path.Combine(_basePath, fileName);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("YAML file not found: {Path}", fullPath);
                return new YamlDocument();
            }

            var yaml = File.ReadAllText(fullPath);
            return _deserializer.Deserialize<YamlDocument>(yaml) ?? new YamlDocument();
        }

        private async Task<YamlDocument> LoadYamlDocumentAsync(string fileName)
        {
            var fullPath = Path.Combine(_basePath, fileName);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("YAML file not found: {Path}", fullPath);
                return new YamlDocument();
            }

            var yaml = await File.ReadAllTextAsync(fullPath);
            return _deserializer.Deserialize<YamlDocument>(yaml) ?? new YamlDocument();
        }

        private T NavigateAndExtract<T>(YamlDocument document, string path)
        {
            var parts = path.Split('.');
            object current = document.Data;

            foreach (var part in parts)
            {
                if (current == null) return default;

                switch (current)
                {
                    case IDictionary<string, object> dict:
                        current = dict.TryGetValue(part, out var value) ? value : null;
                        break;
                    case IDictionary<object, object> objDict:
                        current = objDict.TryGetValue(part, out var objValue) ? objValue : null;
                        break;
                    default:
                        return default;
                }
            }

            return current is T typedValue ? typedValue : default;
        }

        public void ReloadCache()
        {
            _cache.Clear();
            _logger.LogInformation("YAML cache cleared");
        }

        public void ReloadFile(string fileName)
        {
            _cache.TryRemove(fileName, out _);
            _logger.LogInformation("Removed {FileName} from cache", fileName);
        }
    }

    // Helper class to store deserialized YAML data
    public class YamlDocument
    {
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
```

### Usage Examples

```csharp
// In your Startup or Program.cs
services.AddSingleton<IYamlSettingsCache>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<YamlSettingsCache>>();
    return new YamlSettingsCache(logger);
});

// Using the cache in your services
public class ScanOrchestrator
{
    private readonly IYamlSettingsCache _yamlCache;

    public ScanOrchestrator(IYamlSettingsCache yamlCache)
    {
        _yamlCache = yamlCache;
    }

    public async Task InitializeAsync()
    {
        // Get settings from YAML files
        var crashgenName = _yamlCache.GetSetting<string>(
            "CLASSIC Main.yaml", 
            "CrashgenInfo.Name", 
            "Unknown");

        var ignoredPlugins = _yamlCache.GetSetting<List<string>>(
            "CLASSIC Fallout4.yaml", 
            "CLASSIC_Ignore_Fallout4", 
            new List<string>());

        var useAsyncPipeline = await _yamlCache.GetSettingAsync<bool>(
            "CLASSIC Main.yaml", 
            "INI Folder Path.Use Async Pipeline", 
            true);
    }
}
```

## Option 3: Strongly Typed Configuration Classes

For better type safety, create configuration classes that map to your YAML structure:

```csharp
public class ClassicConfiguration
{
    public IniFolderPath IniFolderPath { get; set; }
    public ClassicInterface ClassicInterface { get; set; }
    public List<string> ClassicAutoBackup { get; set; }
}

public class IniFolderPath
{
    public string ModsFolderPath { get; set; }
    public string ScanCustomPath { get; set; }
    public bool AudioNotifications { get; set; }
    public string UpdateSource { get; set; }
    public bool UseAsyncPipeline { get; set; }
    public bool DisableCliProgress { get; set; }
}

// Load configuration
public class ConfigurationService
{
    private readonly IDeserializer _deserializer;

    public ConfigurationService()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public T LoadConfiguration<T>(string yamlPath) where T : class, new()
    {
        if (!File.Exists(yamlPath))
            return new T();

        var yaml = File.ReadAllText(yamlPath);
        return _deserializer.Deserialize<T>(yaml) ?? new T();
    }
}
```

## Recommendation

For the CLASSIC port, I recommend **Option 2** (Direct YAML Settings Cache) because:

1. It most closely matches the Python implementation
2. It provides the same dynamic access patterns
3. It supports the existing YAML file structure without modification
4. It includes caching and async support
5. It's more flexible for accessing nested properties

The implementation can be enhanced with:
- File watching for automatic reload
- Better error handling for missing keys
- Support for complex type conversions
- Performance optimizations for frequent access patterns