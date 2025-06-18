using System.Collections.Concurrent;
using NLog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CLASSIC_8.Core.Yaml;

public sealed class YamlSettingsCache(IGameManager gameManager) : IYamlSettingsCache
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly HashSet<YamlStore> StaticYamlStores = [YamlStore.Main, YamlStore.Game];
    
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private readonly ConcurrentDictionary<string, DateTime> _fileModTimes = new();
    private readonly ConcurrentDictionary<YamlStore, string> _pathCache = new();
    private readonly ConcurrentDictionary<(YamlStore, string, Type), object?> _settingsCache = new();

    public string GetPathForStore(YamlStore yamlStore)
    {
        if (_pathCache.TryGetValue(yamlStore, out var cachedPath))
        {
            return cachedPath;
        }
        
        const string dataPath = "CLASSIC Data";
        string yamlPath = yamlStore switch
        {
            YamlStore.Main => Path.Combine(dataPath, "databases", "CLASSIC Main.yaml"),
            YamlStore.Settings => "CLASSIC Settings.yaml",
            YamlStore.Ignore => "CLASSIC Ignore.yaml",
            YamlStore.Game => Path.Combine(dataPath, "databases", $"CLASSIC {gameManager.CurrentGame}.yaml"),
            YamlStore.GameLocal => Path.Combine(dataPath, $"CLASSIC {gameManager.CurrentGame} Local.yaml"),
            YamlStore.Test => Path.Combine("tests", "test_settings.yaml"),
            _ => throw new NotImplementedException($"YAML store {yamlStore} is not implemented")
        };
        
        _pathCache.TryAdd(yamlStore, yamlPath);
        return yamlPath;
    }
    
    public T? GetSetting<T>(YamlStore yamlStore, string keyPath, T? newValue = default)
    {
        var cacheKey = (yamlStore, keyPath, typeof(T));
        
        // If this is a read operation for a static store, check cache first
        if (newValue == null && StaticYamlStores.Contains(yamlStore) && _settingsCache.TryGetValue(cacheKey, out var cachedValue))
        {
            return (T?)cachedValue;
        }
        
        var yamlPath = GetPathForStore(yamlStore);
        var data = LoadYaml(yamlPath, yamlStore);
        var keys = keyPath.Split('.');
        
        try
        {
            var current = data;
            
            // Navigate to the parent container
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (current is not IDictionary<object, object> currentDict)
                {
                    if (newValue != null)
                    {
                        // Create missing parent dictionaries when writing
                        currentDict = new Dictionary<object, object>();
                        if (data is IDictionary<object, object> rootDict && i == 0)
                        {
                            rootDict[keys[0]] = currentDict;
                            data = rootDict;
                        }
                    }
                    else
                    {
                        Logger.Warn($"Path not found: {string.Join(".", keys.Take(i + 1))} in {yamlStore}");
                        return default;
                    }
                }
                
                if (!currentDict.ContainsKey(keys[i]))
                {
                    if (newValue != null)
                    {
                        // Create missing key when writing
                        currentDict[keys[i]] = new Dictionary<object, object>();
                    }
                    else
                    {
                        Logger.Warn($"Path not found: {string.Join(".", keys.Take(i + 1))} in {yamlStore}");
                        return default;
                    }
                }
                
                current = currentDict[keys[i]];
            }
            
            var lastKey = keys[^1];
            
            // Handle write operation
            if (newValue != null)
            {
                if (StaticYamlStores.Contains(yamlStore))
                {
                    var error = $"Attempting to modify static YAML store {yamlStore} at {keyPath}";
                    Logger.Error(error);
                    throw new InvalidOperationException(error);
                }
                
                if (current is IDictionary<object, object> container)
                {
                    container[lastKey] = newValue;
                    
                    // Write changes back to file
                    File.WriteAllText(yamlPath, _serializer.Serialize(data));
                    
                    // Update cache
                    _cache[yamlPath] = data;
                    _fileModTimes[yamlPath] = File.GetLastWriteTime(yamlPath);
                    
                    // Clear cached setting
                    _settingsCache.TryRemove(cacheKey, out _);
                    
                    return newValue;
                }
            }
            
            // Handle read operation
            if (current is IDictionary<object, object> readContainer && readContainer.TryGetValue(lastKey, out var value))
            {
                var result = ConvertValue<T>(value);
                
                // Cache the result for static stores
                if (StaticYamlStores.Contains(yamlStore))
                {
                    _settingsCache.TryAdd(cacheKey, result);
                }
                
                return result;
            }
            
            Logger.Warn($"Key not found: {keyPath} in {yamlStore}");
            return default;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error accessing setting {keyPath} in {yamlStore}");
            return default;
        }
    }
    
    public void ReloadYamlFile(YamlStore yamlStore)
    {
        var yamlPath = GetPathForStore(yamlStore);
        _cache.TryRemove(yamlPath, out _);
        _fileModTimes.TryRemove(yamlPath, out _);
        
        // Clear all cached settings for this store
        var keysToRemove = _settingsCache.Keys.Where(k => k.Item1 == yamlStore).ToList();
        foreach (var key in keysToRemove)
        {
            _settingsCache.TryRemove(key, out _);
        }
    }
    
    private object LoadYaml(string yamlPath, YamlStore yamlStore)
    {
        if (!File.Exists(yamlPath))
        {
            return new Dictionary<object, object>();
        }
        
        var isStatic = StaticYamlStores.Contains(yamlStore);
        
        if (isStatic)
        {
            // For static files, just load once
            if (!_cache.ContainsKey(yamlPath))
            {
                Logger.Debug($"Loading static YAML file: {yamlPath}");
                var content = File.ReadAllText(yamlPath);
                var data = _deserializer.Deserialize<object>(content);
                _cache[yamlPath] = data;
            }
        }
        else
        {
            // For dynamic files, check modification time
            var lastModTime = File.GetLastWriteTime(yamlPath);
            
            if (!_fileModTimes.TryGetValue(yamlPath, out var cachedModTime) || cachedModTime != lastModTime)
            {
                Logger.Debug($"Loading dynamic YAML file: {yamlPath}");
                var content = File.ReadAllText(yamlPath);
                var data = _deserializer.Deserialize<object>(content);
                
                _cache[yamlPath] = data;
                _fileModTimes[yamlPath] = lastModTime;
            }
        }
        
        return _cache[yamlPath];
    }
    
    private static T? ConvertValue<T>(object? value)
    {
        if (value == null)
        {
            return default;
        }
        
        var targetType = typeof(T);
        
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        try
        {
            // Direct type match
            if (value.GetType() == underlyingType)
            {
                return (T)value;
            }
            
            // Handle string to Path conversion
            if (underlyingType == typeof(FileInfo) && value is string strPath)
            {
                return (T)(object)new FileInfo(strPath);
            }
            
            if (underlyingType == typeof(DirectoryInfo) && value is string dirPath)
            {
                return (T)(object)new DirectoryInfo(dirPath);
            }
            
            // Handle lists
            if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = underlyingType.GetGenericArguments()[0];
                if (value is IEnumerable<object> enumerable)
                {
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var list = Activator.CreateInstance(listType);
                    var addMethod = listType.GetMethod("Add");
                    
                    foreach (var item in enumerable)
                    {
                        var convertedItem = Convert.ChangeType(item, elementType);
                        addMethod?.Invoke(list, new[] { convertedItem });
                    }
                    
                    return (T)list!;
                }
            }
            
            // Try standard conversion
            return (T)Convert.ChangeType(value, underlyingType);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to convert value '{value}' to type {targetType}");
            return default;
        }
    }
}