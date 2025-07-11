using Classic.ScanLog.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;
using YamlDotNet.Serialization;

namespace Classic.ScanLog.Configuration;

/// <summary>
/// Loads game hints and report configuration from YAML
/// </summary>
public class GameHintsLoader
{
    private readonly ILogger<GameHintsLoader> _logger;
    private GameHintsConfig? _cachedConfig;

    public GameHintsLoader(ILogger<GameHintsLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads game hints configuration from embedded YAML resource
    /// </summary>
    public async Task<GameHintsConfig> LoadGameHintsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Classic.ScanLog.Data.GameHints.yaml";

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogError("Game hints YAML resource not found: {ResourceName}", resourceName);
                return CreateDefaultConfig();
            }

            using var reader = new StreamReader(stream);
            var yamlContent = await reader.ReadToEndAsync(cancellationToken);

            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            var config = deserializer.Deserialize<GameHintsConfig>(yamlContent);
            
            if (config?.GameHints?.Count > 0)
            {
                _cachedConfig = config;
                _logger.LogInformation("Loaded {Count} game hints from YAML", config.GameHints.Count);
                return config;
            }
            else
            {
                _logger.LogWarning("No game hints found in YAML configuration");
                return CreateDefaultConfig();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load game hints configuration");
            return CreateDefaultConfig();
        }
    }

    /// <summary>
    /// Gets random game hints for inclusion in reports
    /// </summary>
    public async Task<List<string>> GetRandomHintsAsync(int count = 3, CancellationToken cancellationToken = default)
    {
        var config = await LoadGameHintsAsync(cancellationToken);
        
        if (config.GameHints.Count == 0)
            return new List<string>();

        var random = new Random();
        return config.GameHints
            .OrderBy(x => random.Next())
            .Take(Math.Min(count, config.GameHints.Count))
            .ToList();
    }

    /// <summary>
    /// Gets GPU-specific recommendations
    /// </summary>
    public async Task<List<string>> GetGpuRecommendationsAsync(GpuManufacturer manufacturer, CancellationToken cancellationToken = default)
    {
        var config = await LoadGameHintsAsync(cancellationToken);
        
        return manufacturer switch
        {
            GpuManufacturer.Nvidia => config.GpuSpecific?.Nvidia?.Recommendations ?? new List<string>(),
            GpuManufacturer.Amd => config.GpuSpecific?.Amd?.Recommendations ?? new List<string>(),
            GpuManufacturer.Intel => config.GpuSpecific?.Intel?.Recommendations ?? new List<string>(),
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Gets warning configuration by type
    /// </summary>
    public async Task<WarningConfig?> GetWarningConfigAsync(string warningType, CancellationToken cancellationToken = default)
    {
        var config = await LoadGameHintsAsync(cancellationToken);
        
        return warningType.ToLowerInvariant() switch
        {
            "plugin_limit" => config.Warnings?.PluginLimit,
            "outdated_crashgen" => config.Warnings?.OutdatedCrashgen,
            "missing_essential_mods" => config.Warnings?.MissingEssentialMods,
            _ => null
        };
    }

    /// <summary>
    /// Creates a default configuration if YAML loading fails
    /// </summary>
    private GameHintsConfig CreateDefaultConfig()
    {
        return new GameHintsConfig
        {
            GameHints = new List<string>
            {
                "Keep your game and mods updated for better stability.",
                "Always backup your saves before making major changes.",
                "Use Mod Organizer 2 for better mod management.",
                "F4SE and Buffout 4 are essential for a stable modded game.",
                "Large settlements can cause performance issues."
            },
            Warnings = new WarningsConfig
            {
                PluginLimit = new WarningConfig
                {
                    Title = "CRITICAL: Plugin Limit Reached",
                    Message = "You have reached the 254 plugin limit. This can cause game instability."
                },
                OutdatedCrashgen = new WarningConfig
                {
                    Title = "WARNING: Outdated Crash Reporter",
                    Message = "Your crash reporting tool is outdated. Please update for better analysis."
                },
                MissingEssentialMods = new WarningConfig
                {
                    Title = "RECOMMENDATION: Essential Mods Missing",
                    Message = "Consider installing essential stability mods for better experience."
                }
            },
            ReportSections = new ReportSectionsConfig
            {
                Header = new ReportSectionConfig
                {
                    Title = "CLASSIC-8 Crash Log Analysis Report",
                    Subtitle = "Comprehensive Analysis and Recommendations"
                },
                Footer = new ReportFooterConfig
                {
                    GeneratedBy = "Generated by CLASSIC-8 v{version}",
                    Documentation = "For more help, visit the documentation."
                }
            }
        };
    }
}

/// <summary>
/// Game hints configuration structure
/// </summary>
public class GameHintsConfig
{
    public List<string> GameHints { get; set; } = new();
    public WarningsConfig? Warnings { get; set; }
    public ReportSectionsConfig? ReportSections { get; set; }
    public PerformanceMetricsConfig? PerformanceMetrics { get; set; }
    public GpuSpecificConfig? GpuSpecific { get; set; }
}

/// <summary>
/// Warnings configuration
/// </summary>
public class WarningsConfig
{
    public WarningConfig? PluginLimit { get; set; }
    public WarningConfig? OutdatedCrashgen { get; set; }
    public WarningConfig? MissingEssentialMods { get; set; }
}

/// <summary>
/// Individual warning configuration
/// </summary>
public class WarningConfig
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Report sections configuration
/// </summary>
public class ReportSectionsConfig
{
    public ReportSectionConfig? Header { get; set; }
    public ReportFooterConfig? Footer { get; set; }
}

/// <summary>
/// Report section configuration
/// </summary>
public class ReportSectionConfig
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string? Instructions { get; set; }
}

/// <summary>
/// Report footer configuration
/// </summary>
public class ReportFooterConfig
{
    public string GeneratedBy { get; set; } = string.Empty;
    public string? Documentation { get; set; }
    public string? Community { get; set; }
}

/// <summary>
/// Performance metrics configuration
/// </summary>
public class PerformanceMetricsConfig
{
    public double ScanTimeExcellent { get; set; } = 2.0;
    public double ScanTimeGood { get; set; } = 5.0;
    public double ScanTimePoor { get; set; } = 10.0;
}

/// <summary>
/// GPU-specific configuration
/// </summary>
public class GpuSpecificConfig
{
    public GpuConfig? Nvidia { get; set; }
    public GpuConfig? Amd { get; set; }
    public GpuConfig? Intel { get; set; }
}

/// <summary>
/// GPU configuration
/// </summary>
public class GpuConfig
{
    public List<string> Recommendations { get; set; } = new();
    public List<string> WarningMods { get; set; } = new();
}