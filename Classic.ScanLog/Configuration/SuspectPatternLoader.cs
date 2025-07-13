using System.Reflection;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Classic.ScanLog.Configuration;

/// <summary>
/// Loads suspect patterns from YAML configuration files
/// </summary>
public class SuspectPatternLoader
{
    private readonly ILogger<SuspectPatternLoader> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public SuspectPatternLoader(ILogger<SuspectPatternLoader> logger)
    {
        _logger = logger;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Loads suspect patterns from embedded YAML resources
    /// </summary>
    /// <returns>Dictionary containing all loaded suspect patterns</returns>
    public async Task<SuspectPatternDatabase> LoadSuspectPatternsAsync()
    {
        try
        {
            _logger.LogDebug("Loading suspect patterns from embedded resources");

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Classic.ScanLog.Data.SuspectPatterns.yaml";

            // Debug: List all available resources
            var resourceNames = assembly.GetManifestResourceNames();
            _logger.LogDebug("Available embedded resources: {Resources}", string.Join(", ", resourceNames));

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogWarning(
                    "Could not find embedded suspect patterns resource: {ResourceName}. Available resources: {Available}",
                    resourceName, string.Join(", ", resourceNames));
                return CreateEmptyDatabase();
            }

            using var reader = new StreamReader(stream);
            var yamlContent = await reader.ReadToEndAsync();

            var database = _yamlDeserializer.Deserialize<SuspectPatternDatabase>(yamlContent);

            _logger.LogInformation("Loaded {ErrorCount} error patterns and {StackCount} stack patterns",
                database.CrashlogErrorCheck?.Count ?? 0,
                database.CrashlogStackCheck?.Count ?? 0);

            return database ?? CreateEmptyDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load suspect patterns");
            return CreateEmptyDatabase();
        }
    }

    /// <summary>
    /// Loads suspect patterns from a file path
    /// </summary>
    /// <param name="filePath">Path to the YAML file</param>
    /// <returns>Dictionary containing all loaded suspect patterns</returns>
    public async Task<SuspectPatternDatabase> LoadSuspectPatternsFromFileAsync(string filePath)
    {
        try
        {
            _logger.LogDebug("Loading suspect patterns from file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Suspect patterns file not found: {FilePath}", filePath);
                return CreateEmptyDatabase();
            }

            var yamlContent = await File.ReadAllTextAsync(filePath);
            var database = _yamlDeserializer.Deserialize<SuspectPatternDatabase>(yamlContent);

            _logger.LogInformation("Loaded {ErrorCount} error patterns and {StackCount} stack patterns from {FilePath}",
                database.CrashlogErrorCheck?.Count ?? 0,
                database.CrashlogStackCheck?.Count ?? 0,
                filePath);

            return database ?? CreateEmptyDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load suspect patterns from file: {FilePath}", filePath);
            return CreateEmptyDatabase();
        }
    }

    /// <summary>
    /// Creates an empty database as fallback
    /// </summary>
    private static SuspectPatternDatabase CreateEmptyDatabase()
    {
        return new SuspectPatternDatabase
        {
            CrashlogErrorCheck = new Dictionary<string, string>(),
            CrashlogStackCheck = new Dictionary<string, List<string>>()
        };
    }
}

/// <summary>
/// Represents the suspect pattern database structure from YAML
/// </summary>
public class SuspectPatternDatabase
{
    public Dictionary<string, string> CrashlogErrorCheck { get; set; } = new();
    public Dictionary<string, List<string>> CrashlogStackCheck { get; set; } = new();
}

/// <summary>
/// Configuration model for suspect patterns
/// </summary>
public class SuspectPatternsConfiguration
{
    /// <summary>
    /// Path to custom suspect patterns file (optional)
    /// </summary>
    public string? CustomPatternsFilePath { get; set; }

    /// <summary>
    /// Whether to load embedded patterns as fallback
    /// </summary>
    public bool UseEmbeddedPatterns { get; set; } = true;

    /// <summary>
    /// Maximum severity level to process (1-6)
    /// </summary>
    public int MaxSeverityLevel { get; set; } = 6;

    /// <summary>
    /// Minimum severity level to process (1-6)
    /// </summary>
    public int MinSeverityLevel { get; set; } = 1;
}
