using System.Reflection;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Classic.ScanLog.Models;

namespace Classic.ScanLog.Configuration;

/// <summary>
/// Loads mod conflict databases from YAML configuration files
/// </summary>
public class ModDatabaseLoader
{
    private readonly ILogger<ModDatabaseLoader> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public ModDatabaseLoader(ILogger<ModDatabaseLoader> logger)
    {
        _logger = logger;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Loads mod conflict database from embedded YAML resources
    /// </summary>
    /// <returns>Mod conflict database containing all loaded patterns</returns>
    public async Task<ModConflictDatabase> LoadModDatabaseAsync()
    {
        try
        {
            _logger.LogDebug("Loading mod conflict database from embedded resources");

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Classic.ScanLog.Data.ModConflictPatterns.yaml";

            // Debug: List all available resources
            var resourceNames = assembly.GetManifestResourceNames();
            _logger.LogDebug("Available embedded resources: {Resources}", string.Join(", ", resourceNames));

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogWarning(
                    "Could not find embedded mod database resource: {ResourceName}. Available resources: {Available}",
                    resourceName, string.Join(", ", resourceNames));
                return CreateEmptyDatabase();
            }

            using var reader = new StreamReader(stream);
            var yamlContent = await reader.ReadToEndAsync();

            var database = _yamlDeserializer.Deserialize<ModConflictDatabase>(yamlContent);

            _logger.LogInformation(
                "Loaded mod database: {CoreCount} core mods, {FreqCount} frequent crash mods, {ConfCount} conflict pairs, {SoluCount} solvable mods",
                database.ModsCore?.Count ?? 0,
                database.ModsFreq?.Count ?? 0,
                database.ModsConf?.Count ?? 0,
                database.ModsSolu?.Count ?? 0);

            return database ?? CreateEmptyDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mod conflict database");
            return CreateEmptyDatabase();
        }
    }

    /// <summary>
    /// Loads mod conflict database from a file path
    /// </summary>
    /// <param name="filePath">Path to the YAML file</param>
    /// <returns>Mod conflict database containing all loaded patterns</returns>
    public async Task<ModConflictDatabase> LoadModDatabaseFromFileAsync(string filePath)
    {
        try
        {
            _logger.LogDebug("Loading mod conflict database from file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Mod database file not found: {FilePath}", filePath);
                return CreateEmptyDatabase();
            }

            var yamlContent = await File.ReadAllTextAsync(filePath);
            var database = _yamlDeserializer.Deserialize<ModConflictDatabase>(yamlContent);

            _logger.LogInformation(
                "Loaded mod database from {FilePath}: {CoreCount} core mods, {FreqCount} frequent crash mods, {ConfCount} conflict pairs, {SoluCount} solvable mods",
                filePath,
                database.ModsCore?.Count ?? 0,
                database.ModsFreq?.Count ?? 0,
                database.ModsConf?.Count ?? 0,
                database.ModsSolu?.Count ?? 0);

            return database ?? CreateEmptyDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mod database from file: {FilePath}", filePath);
            return CreateEmptyDatabase();
        }
    }

    /// <summary>
    /// Creates an empty database as fallback
    /// </summary>
    private static ModConflictDatabase CreateEmptyDatabase()
    {
        return new ModConflictDatabase
        {
            ModsCore = new Dictionary<string, object>(),
            ModsFreq = new Dictionary<string, object>(),
            ModsConf = new Dictionary<string, object>(),
            ModsSolu = new Dictionary<string, object>(),
            GpuCompatibility = new GpuCompatibility(),
            LoadOrderWarnings = new Dictionary<string, string>()
        };
    }
}

/// <summary>
/// Configuration model for mod database settings
/// </summary>
public class ModDatabaseConfiguration
{
    /// <summary>
    /// Path to custom mod database file (optional)
    /// </summary>
    public string? CustomDatabaseFilePath { get; set; }

    /// <summary>
    /// Whether to load embedded database as fallback
    /// </summary>
    public bool UseEmbeddedDatabase { get; set; } = true;

    /// <summary>
    /// Whether to check for GPU-specific mod compatibility
    /// </summary>
    public bool EnableGpuCompatibilityChecks { get; set; } = true;

    /// <summary>
    /// Whether to check for load order issues
    /// </summary>
    public bool EnableLoadOrderChecks { get; set; } = true;
}
