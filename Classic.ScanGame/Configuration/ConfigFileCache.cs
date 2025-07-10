using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text;
using Classic.Core.Interfaces;
using Classic.Infrastructure;
using Microsoft.Extensions.Configuration;
using Serilog;
using Ude;

namespace Classic.ScanGame.Configuration;

/// <summary>
/// Cache for configuration files with duplicate detection and management.
/// </summary>
public class ConfigFileCache
{
    private readonly IFileSystem _fileSystem;
    private readonly IYamlSettingsCache _yamlSettings;
    private readonly IGlobalRegistry _globalRegistry;
    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<string, string> _configFiles = new();
    private readonly ConcurrentDictionary<string, IConfigurationRoot> _configCache = new();
    private readonly ConcurrentDictionary<string, List<string>> _duplicateFiles = new();
    private readonly string? _gameRootPath;
    private readonly List<string> _duplicateWhitelist = new() { "F4EE" };

    private ConfigFileCache(
        IFileSystem fileSystem,
        IYamlSettingsCache yamlSettings,
        IGlobalRegistry globalRegistry,
        ILogger logger,
        string? gameRootPath)
    {
        _fileSystem = fileSystem;
        _yamlSettings = yamlSettings;
        _globalRegistry = globalRegistry;
        _logger = logger;
        _gameRootPath = gameRootPath;
    }

    /// <summary>
    /// Creates a new ConfigFileCache instance and scans for configuration files.
    /// </summary>
    public static async Task<ConfigFileCache> CreateAsync(
        IFileSystem fileSystem,
        IYamlSettingsCache yamlSettings,
        IGlobalRegistry globalRegistry,
        ILogger logger)
    {
        var vrSuffix = globalRegistry.IsVrMode ? "VR" : "";
        var gameRootPath = await yamlSettings.GetSettingAsync<string>("Game_Local", $"Game{vrSuffix}_Info.Root_Folder_Game");

        var cache = new ConfigFileCache(fileSystem, yamlSettings, globalRegistry, logger, gameRootPath);
        await cache.ScanConfigurationFilesAsync();
        return cache;
    }

    /// <summary>
    /// Scans the game root directory for configuration files and identifies duplicates.
    /// </summary>
    private async Task ScanConfigurationFilesAsync()
    {
        if (string.IsNullOrEmpty(_gameRootPath) || !_fileSystem.Directory.Exists(_gameRootPath))
        {
            _logger.Warning("Game root path not found or invalid: {GameRootPath}", _gameRootPath);
            return;
        }

        var fileHashes = new Dictionary<string, string>();

        try
        {
            await ScanDirectoryAsync(_gameRootPath, fileHashes);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error scanning configuration files in {GameRootPath}", _gameRootPath);
        }
    }

    /// <summary>
    /// Recursively scans a directory for configuration files.
    /// </summary>
    private async Task ScanDirectoryAsync(string directoryPath, Dictionary<string, string> fileHashes)
    {
        var directories = _fileSystem.Directory.EnumerateDirectories(directoryPath);
        var files = _fileSystem.Directory.EnumerateFiles(directoryPath);

        // Process files in current directory
        foreach (var file in files)
        {
            await ProcessConfigurationFileAsync(file, fileHashes);
        }

        // Process subdirectories
        foreach (var directory in directories)
        {
            var directoryName = _fileSystem.Path.GetFileName(directory);
            
            // Skip if directory doesn't match whitelist
            if (!_duplicateWhitelist.Any(whitelist => directoryName.Contains(whitelist, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            await ScanDirectoryAsync(directory, fileHashes);
        }
    }

    /// <summary>
    /// Processes a single configuration file and checks for duplicates.
    /// </summary>
    private async Task ProcessConfigurationFileAsync(string filePath, Dictionary<string, string> fileHashes)
    {
        var fileName = _fileSystem.Path.GetFileName(filePath);
        var fileNameLower = fileName.ToLowerInvariant();

        // Skip non-config files and files not matching specific criteria
        if (!IsConfigurationFile(fileNameLower))
        {
            return;
        }

        var fileHash = await CalculateFileHashAsync(filePath);
        
        if (_configFiles.ContainsKey(fileNameLower))
        {
            var existingFilePath = _configFiles[fileNameLower];
            var existingHash = fileHashes.GetValueOrDefault(fileNameLower);

            if (fileHash == existingHash)
            {
                // Exact duplicate
                _duplicateFiles.AddOrUpdate(fileNameLower, 
                    new List<string> { existingFilePath, filePath },
                    (key, existing) => { existing.Add(filePath); return existing; });
            }
            else
            {
                // Check for similarity
                var isSimilar = await AreSimilarFilesAsync(existingFilePath, filePath);
                if (isSimilar)
                {
                    _duplicateFiles.AddOrUpdate(fileNameLower,
                        new List<string> { existingFilePath, filePath },
                        (key, existing) => { existing.Add(filePath); return existing; });
                }
            }
        }
        else
        {
            // Register new config file
            _configFiles.TryAdd(fileNameLower, filePath);
            fileHashes[fileNameLower] = fileHash;
        }
    }

    /// <summary>
    /// Determines if a file is a configuration file based on its name and extension.
    /// </summary>
    private static bool IsConfigurationFile(string fileNameLower)
    {
        return fileNameLower.EndsWith(".ini") || 
               fileNameLower.EndsWith(".conf") || 
               fileNameLower == "dxvk.conf";
    }

    /// <summary>
    /// Calculates a hash for a file to detect exact duplicates.
    /// </summary>
    private async Task<string> CalculateFileHashAsync(string filePath)
    {
        try
        {
            using var fileStream = _fileSystem.File.OpenRead(filePath);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = await Task.Run(() => sha256.ComputeHash(fileStream));
            return Convert.ToHexString(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to calculate hash for file: {FilePath}", filePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks if two files are similar based on size, modification time, or content comparison.
    /// </summary>
    private async Task<bool> AreSimilarFilesAsync(string file1, string file2)
    {
        try
        {
            var file1Info = _fileSystem.FileInfo.New(file1);
            var file2Info = _fileSystem.FileInfo.New(file2);

            // Check size and modification time
            if (file1Info.Length == file2Info.Length && 
                file1Info.LastWriteTime == file2Info.LastWriteTime)
            {
                return true;
            }

            // For INI files, do a more detailed comparison
            if (file1.EndsWith(".ini", StringComparison.OrdinalIgnoreCase) && 
                file2.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
            {
                return await CompareIniFilesAsync(file1, file2);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error comparing files {File1} and {File2}", file1, file2);
            return false;
        }
    }

    /// <summary>
    /// Compares two INI files to determine if they have identical content.
    /// </summary>
    private async Task<bool> CompareIniFilesAsync(string file1, string file2)
    {
        try
        {
            var config1 = await LoadConfigurationAsync(file1);
            var config2 = await LoadConfigurationAsync(file2);

            if (config1 == null || config2 == null)
            {
                return false;
            }

            // Compare configuration sections and values
            var sections1 = config1.GetChildren().ToList();
            var sections2 = config2.GetChildren().ToList();

            if (sections1.Count != sections2.Count)
            {
                return false;
            }

            for (var i = 0; i < sections1.Count; i++)
            {
                if (!ConfigurationSectionsEqual(sections1[i], sections2[i]))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error comparing INI files {File1} and {File2}", file1, file2);
            return false;
        }
    }

    /// <summary>
    /// Compares two configuration sections for equality.
    /// </summary>
    private static bool ConfigurationSectionsEqual(IConfigurationSection section1, IConfigurationSection section2)
    {
        if (section1.Key != section2.Key)
        {
            return false;
        }

        var children1 = section1.GetChildren().ToList();
        var children2 = section2.GetChildren().ToList();

        if (children1.Count != children2.Count)
        {
            return false;
        }

        for (var i = 0; i < children1.Count; i++)
        {
            if (children1[i].Key != children2[i].Key || children1[i].Value != children2[i].Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Loads a configuration file into an IConfigurationRoot.
    /// </summary>
    private async Task<IConfigurationRoot?> LoadConfigurationAsync(string filePath)
    {
        try
        {
            var fileNameLower = _fileSystem.Path.GetFileName(filePath).ToLowerInvariant();
            
            if (_configCache.TryGetValue(fileNameLower, out var cachedConfig))
            {
                return cachedConfig;
            }

            var fileContent = await ReadFileWithEncodingAsync(filePath);
            var configBuilder = new ConfigurationBuilder();
            
            // Add appropriate configuration provider based on file extension
            if (filePath.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
            {
                // For INI files, use a custom provider or parse manually
                var config = ParseIniConfiguration(fileContent);
                _configCache.TryAdd(fileNameLower, config);
                return config;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load configuration file: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Parses INI file content into a configuration object.
    /// </summary>
    private IConfigurationRoot ParseIniConfiguration(string content)
    {
        var data = new Dictionary<string, string?>();
        var lines = content.Split('\n');
        string? currentSection = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(';') || trimmedLine.StartsWith('#'))
            {
                continue;
            }

            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                currentSection = trimmedLine[1..^1];
                continue;
            }

            var equalIndex = trimmedLine.IndexOf('=');
            if (equalIndex > 0 && !string.IsNullOrEmpty(currentSection))
            {
                var key = trimmedLine[..equalIndex].Trim();
                var value = trimmedLine[(equalIndex + 1)..].Trim();
                data[$"{currentSection}:{key}"] = value;
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    /// <summary>
    /// Reads a file with automatic encoding detection.
    /// </summary>
    private async Task<string> ReadFileWithEncodingAsync(string filePath)
    {
        var fileBytes = await _fileSystem.File.ReadAllBytesAsync(filePath);
        
        // Detect encoding
        var detector = new CharsetDetector();
        detector.Feed(fileBytes, 0, fileBytes.Length);
        detector.DataEnd();

        var encoding = Encoding.UTF8; // Default fallback
        if (detector.Charset != null)
        {
            try
            {
                encoding = Encoding.GetEncoding(detector.Charset);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to get encoding {Charset}, using UTF-8", detector.Charset);
            }
        }

        return encoding.GetString(fileBytes);
    }

    /// <summary>
    /// Checks if a file exists in the cache.
    /// </summary>
    public bool HasFile(string fileName)
    {
        return _configFiles.ContainsKey(fileName.ToLowerInvariant());
    }

    /// <summary>
    /// Gets the file path for a configuration file.
    /// </summary>
    public string? GetFilePath(string fileName)
    {
        return _configFiles.GetValueOrDefault(fileName.ToLowerInvariant());
    }

    /// <summary>
    /// Gets all configuration files.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>> GetFiles()
    {
        return _configFiles;
    }

    /// <summary>
    /// Gets duplicate files.
    /// </summary>
    public async Task<Dictionary<string, List<string>>> GetDuplicateFilesAsync()
    {
        await Task.CompletedTask;
        return _duplicateFiles.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Checks if a setting exists in a configuration file.
    /// </summary>
    public async Task<bool> HasSettingAsync(string fileName, string section, string key)
    {
        var config = await LoadConfigurationAsync(GetFilePath(fileName.ToLowerInvariant()) ?? "");
        return config?.GetSection($"{section}:{key}").Exists() == true;
    }

    /// <summary>
    /// Gets a setting value from a configuration file.
    /// </summary>
    public async Task<T?> GetSettingAsync<T>(string fileName, string section, string key)
    {
        var config = await LoadConfigurationAsync(GetFilePath(fileName.ToLowerInvariant()) ?? "");
        if (config == null)
        {
            return default;
        }

        var value = config.GetSection($"{section}:{key}").Value;
        if (value == null)
        {
            return default;
        }

        try
        {
            return (T?)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to convert setting value {Value} to type {Type}", value, typeof(T));
            return default;
        }
    }

    /// <summary>
    /// Sets a setting value in a configuration file.
    /// </summary>
    public async Task SetSettingAsync(string fileName, string section, string key, object value)
    {
        var filePath = GetFilePath(fileName.ToLowerInvariant());
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        try
        {
            var configManager = new IniConfigurationManager(_fileSystem, _logger);
            await configManager.SetSettingAsync(filePath, section, key, value);
            
            // Invalidate cache
            _configCache.TryRemove(fileName.ToLowerInvariant(), out _);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to set setting {Section}:{Key} = {Value} in {FilePath}", section, key, value, filePath);
        }
    }
}