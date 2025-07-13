using System.Collections.Concurrent;
using System.Text;
using Classic.Core.Enums;
using Classic.Core.Interfaces;
using Serilog;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Classic.Infrastructure.Configuration;

/// <summary>
/// Provides a comprehensive YAML settings management system with caching and persistence support.
/// </summary>
public class YamlSettings : IYamlSettings
{
    private readonly ILogger _logger;
    private readonly string _dataDirectory;
    private readonly ConcurrentDictionary<YamlStore, YamlDocument> _documents = new();
    private readonly ConcurrentDictionary<string, object?> _cache = new();
    private readonly ConcurrentDictionary<YamlStore, DateTime> _lastModified = new();
    private readonly HashSet<YamlStore> _dirtyStores = [];
    private readonly object _lock = new();

    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    // Static stores that shouldn't be modified
    private static readonly HashSet<YamlStore> StaticStores = [YamlStore.Main, YamlStore.Game];

    public YamlSettings(ILogger logger, string? dataDirectory = null)
    {
        _logger = logger;
        _dataDirectory = dataDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CLASSIC Data");

        _serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        // Ensure data directory exists
        Directory.CreateDirectory(_dataDirectory);
        Directory.CreateDirectory(Path.Combine(_dataDirectory, "databases"));
    }

    public T? Get<T>(YamlStore store, string path, T? defaultValue = default)
    {
        var cacheKey = GetCacheKey(store, path);

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out var cachedValue))
        {
            return (T?)cachedValue;
        }

        lock (_lock)
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue(cacheKey, out cachedValue))
            {
                return (T?)cachedValue;
            }

            try
            {
                var document = LoadDocument(store);
                var value = GetValueFromDocument(document, path, defaultValue);

                // Cache the result
                _cache.TryAdd(cacheKey, value);

                return value;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting setting {Path} from {Store}", path, store);
                return defaultValue;
            }
        }
    }

    public async Task<T?> GetAsync<T>(YamlStore store, string path, T? defaultValue = default)
    {
        return await Task.Run(() => Get<T>(store, path, defaultValue));
    }

    public void Set<T>(YamlStore store, string path, T value)
    {
        if (StaticStores.Contains(store))
        {
            throw new InvalidOperationException($"Cannot modify static YAML store: {store}");
        }

        lock (_lock)
        {
            try
            {
                var document = LoadDocument(store);
                SetValueInDocument(document, path, value);

                // Update cache
                var cacheKey = GetCacheKey(store, path);
                _cache.AddOrUpdate(cacheKey, value, (_, _) => value);

                // Mark store as dirty
                _dirtyStores.Add(store);

                _logger.Debug("Set {Path} in {Store} to {Value}", path, store, value);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error setting {Path} in {Store}", path, store);
                throw;
            }
        }
    }

    public async Task SetAsync<T>(YamlStore store, string path, T value)
    {
        await Task.Run(() => Set(store, path, value));
    }

    public bool Exists(YamlStore store, string path)
    {
        try
        {
            var document = LoadDocument(store);
            return GetValueFromDocument<object>(document, path, null) != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task SaveAsync()
    {
        var storesToSave = _dirtyStores.ToList();

        foreach (var store in storesToSave)
        {
            try
            {
                await SaveDocumentAsync(store);
                _dirtyStores.Remove(store);
                _logger.Information("Saved changes to {Store}", store);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save {Store}", store);
                throw;
            }
        }
    }

    public void Reload()
    {
        lock (_lock)
        {
            _cache.Clear();
            _documents.Clear();
            _lastModified.Clear();
            _dirtyStores.Clear();
            _logger.Information("Reloaded all YAML settings");
        }
    }

    public string GetStorePath(YamlStore store)
    {
        return store switch
        {
            YamlStore.Main => Path.Combine(_dataDirectory, "databases", "CLASSIC Main.yaml"),
            YamlStore.Settings => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CLASSIC Settings.yaml"),
            YamlStore.Ignore => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CLASSIC Ignore.yaml"),
            YamlStore.Game => Path.Combine(_dataDirectory, "databases", $"CLASSIC {GetCurrentGame()}.yaml"),
            YamlStore.GameLocal => Path.Combine(_dataDirectory, $"CLASSIC {GetCurrentGame()} Local.yaml"),
            YamlStore.Test => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_settings.yaml"),
            _ => throw new ArgumentException($"Unknown YAML store: {store}")
        };
    }

    private string GetCurrentGame()
    {
        // Try to get from settings, default to Fallout 4
        var game = Get<string>(YamlStore.Settings, "CLASSIC_Settings.Managed Game", "Fallout 4");
        return game ?? "Fallout 4";
    }

    private YamlDocument LoadDocument(YamlStore store)
    {
        // Check if document is already loaded and up-to-date
        if (_documents.TryGetValue(store, out var document))
        {
            var filePath = GetStorePath(store);
            if (File.Exists(filePath))
            {
                var lastWriteTime = File.GetLastWriteTime(filePath);
                if (_lastModified.TryGetValue(store, out var cachedTime) && lastWriteTime <= cachedTime)
                {
                    return document;
                }
            }
        }

        // Load or reload the document
        return LoadDocumentFromDisk(store);
    }

    private YamlDocument LoadDocumentFromDisk(YamlStore store)
    {
        var filePath = GetStorePath(store);

        if (!File.Exists(filePath))
        {
            // Create default file if it doesn't exist
            CreateDefaultFile(store, filePath);
        }

        try
        {
            var yaml = File.ReadAllText(filePath);
            var stream = new YamlStream();
            using (var reader = new StringReader(yaml))
            {
                stream.Load(reader);
            }

            var document = stream.Documents.FirstOrDefault() ?? new YamlDocument(new YamlMappingNode());

            _documents.AddOrUpdate(store, document, (_, _) => document);
            _lastModified.AddOrUpdate(store, File.GetLastWriteTime(filePath),
                (_, _) => File.GetLastWriteTime(filePath));

            return document;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load YAML document from {FilePath}", filePath);
            throw;
        }
    }

    private void CreateDefaultFile(YamlStore store, string filePath)
    {
        _logger.Information("Creating default file for {Store} at {FilePath}", store, filePath);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var defaultContent = GetDefaultContent(store);
        File.WriteAllText(filePath, defaultContent, Encoding.UTF8);
    }

    private string GetDefaultContent(YamlStore store)
    {
        return store switch
        {
            YamlStore.Settings => @"# CLASSIC Settings
CLASSIC_Settings:
  Managed Game: Fallout 4
  Update Check: true
  VR Mode: false
  FCX Mode: true
  Simplify Logs: false
  Sound On Completion: true
  Scan Game Files: false
  Exclude Warnings: false
  Use Pre Releases: false
  Update Source: GitHub
  Staging Mods Path: 
  Custom Scan Path: 
  Game Ini Path: 
",
            YamlStore.Ignore => @"# CLASSIC Ignore Lists
Ignore_Lists:
  Fallout 4: []
  Skyrim SE: []
  Skyrim VR: []
  Fallout 4 VR: []
",
            YamlStore.GameLocal => @"# Local game configuration
Game_Info:
  Root Folder: 
  Data Folder: 
  INI Folder: 
",
            _ => "{}"
        };
    }

    private T? GetValueFromDocument<T>(YamlDocument document, string path, T? defaultValue)
    {
        var keys = path.Split('.');
        YamlNode? current = document.RootNode;

        foreach (var key in keys)
        {
            if (current is YamlMappingNode mapping &&
                mapping.Children.TryGetValue(new YamlScalarNode(key), out var child))
            {
                current = child;
            }
            else
            {
                return defaultValue;
            }
        }

        if (current is YamlScalarNode scalar)
        {
            try
            {
                var value = scalar.Value;
                if (typeof(T) == typeof(string))
                    return (T)(object)(value ?? string.Empty);
                if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(value ?? "0");
                if (typeof(T) == typeof(bool))
                    return (T)(object)bool.Parse(value ?? "false");
                if (typeof(T) == typeof(double))
                    return (T)(object)double.Parse(value ?? "0.0");

                // Try to deserialize complex types
                return _deserializer.Deserialize<T>(value ?? string.Empty);
            }
            catch
            {
                return defaultValue;
            }
        }

        // For complex types, serialize the node and deserialize to the target type
        if (current != null)
        {
            try
            {
                using var writer = new StringWriter();
                var stream = new YamlStream(new YamlDocument(current));
                stream.Save(writer, false);
                return _deserializer.Deserialize<T>(writer.ToString());
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    private void SetValueInDocument<T>(YamlDocument document, string path, T value)
    {
        var keys = path.Split('.');
        YamlMappingNode? current = document.RootNode as YamlMappingNode;

        if (current == null)
        {
            // If root node is not a mapping node, we need to replace the document
            current = new YamlMappingNode();
            var newDoc = new YamlDocument(current);
            _documents.TryUpdate(GetStoreFromDocument(document), newDoc, document);
            document = newDoc;
        }

        // Navigate to parent node, creating nodes as needed
        for (int i = 0; i < keys.Length - 1; i++)
        {
            var key = new YamlScalarNode(keys[i]);

            if (current.Children.TryGetValue(key, out var child) && child is YamlMappingNode childMapping)
            {
                current = childMapping;
            }
            else
            {
                var newMapping = new YamlMappingNode();
                current.Children[key] = newMapping;
                current = newMapping;
            }
        }

        // Set the value
        var lastKey = new YamlScalarNode(keys[^1]);
        if (value == null)
        {
            current.Children.Remove(lastKey);
        }
        else
        {
            var valueNode = CreateValueNode(value);
            current.Children[lastKey] = valueNode;
        }
    }

    private YamlNode CreateValueNode<T>(T value)
    {
        if (value is string || value is int || value is bool || value is double || value is float)
        {
            return new YamlScalarNode(value.ToString() ?? string.Empty);
        }

        // For complex types, serialize and parse back as YAML
        var serialized = _serializer.Serialize(value);
        using var reader = new StringReader(serialized);
        var stream = new YamlStream();
        stream.Load(reader);
        return stream.Documents[0].RootNode;
    }

    private async Task SaveDocumentAsync(YamlStore store)
    {
        if (!_documents.TryGetValue(store, out var document))
        {
            return;
        }

        var filePath = GetStorePath(store);
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StringWriter();
        var stream = new YamlStream(document);
        stream.Save(writer, false);

        await File.WriteAllTextAsync(filePath, writer.ToString(), Encoding.UTF8);
        _lastModified.AddOrUpdate(store, DateTime.Now, (_, _) => DateTime.Now);
    }

    private string GetCacheKey(YamlStore store, string path)
    {
        return $"{store}:{path}";
    }

    private YamlStore GetStoreFromDocument(YamlDocument document)
    {
        // Find which store this document belongs to
        foreach (var kvp in _documents)
        {
            if (kvp.Value == document)
                return kvp.Key;
        }

        throw new InvalidOperationException("Document not found in any store");
    }
}