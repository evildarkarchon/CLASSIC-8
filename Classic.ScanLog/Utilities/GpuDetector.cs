using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Classic.Core.Interfaces;
using Classic.ScanLog.Models;

namespace Classic.ScanLog.Utilities;

/// <summary>
/// Detects GPU information from crash log system specifications
/// </summary>
public class GpuDetector : IGpuDetector
{
    private readonly ILogger<GpuDetector> _logger;
    private static readonly Regex GpuRegex = new(@"GPU #(\d+):\s*(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public GpuDetector(ILogger<GpuDetector> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string? Detect()
    {
        // Legacy interface method - could be deprecated in favor of GetGpuInfo
        return null;
    }

    /// <summary>
    /// Extracts GPU information from crash log system specifications
    /// </summary>
    /// <param name="systemSpecs">System specs section from crash log</param>
    /// <returns>GPU information including manufacturer and rival detection</returns>
    public GpuInfo GetGpuInfo(string systemSpecs)
    {
        var gpuInfo = new GpuInfo();

        if (string.IsNullOrEmpty(systemSpecs))
        {
            _logger.LogDebug("No system specs provided for GPU detection");
            return gpuInfo;
        }

        try
        {
            var lines = systemSpecs.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var match = GpuRegex.Match(line.Trim());
                if (!match.Success) continue;

                var gpuNumber = match.Groups[1].Value;
                var gpuDescription = match.Groups[2].Value.Trim();

                _logger.LogDebug("Found GPU #{Number}: {Description}", gpuNumber, gpuDescription);

                if (gpuNumber == "1")
                {
                    gpuInfo.PrimaryGpu = gpuDescription;
                    gpuInfo.Manufacturer = DetermineManufacturer(gpuDescription);
                }
                else if (gpuNumber == "2")
                {
                    gpuInfo.SecondaryGpu = gpuDescription;
                }
            }

            // Set rival manufacturer
            gpuInfo.RivalManufacturer = gpuInfo.Manufacturer switch
            {
                GpuManufacturer.Nvidia => GpuManufacturer.Amd,
                GpuManufacturer.Amd => GpuManufacturer.Nvidia,
                _ => GpuManufacturer.Unknown
            };

            _logger.LogInformation("Detected GPU: {Primary} (Manufacturer: {Manufacturer})", 
                gpuInfo.PrimaryGpu, gpuInfo.Manufacturer);

            if (!string.IsNullOrEmpty(gpuInfo.SecondaryGpu))
            {
                _logger.LogInformation("Secondary GPU: {Secondary}", gpuInfo.SecondaryGpu);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting GPU information from system specs");
        }

        return gpuInfo;
    }

    /// <summary>
    /// Determines GPU manufacturer from description
    /// </summary>
    /// <param name="gpuDescription">GPU description string</param>
    /// <returns>Detected manufacturer</returns>
    private static GpuManufacturer DetermineManufacturer(string gpuDescription)
    {
        if (string.IsNullOrEmpty(gpuDescription))
            return GpuManufacturer.Unknown;

        var description = gpuDescription.ToLowerInvariant();

        // NVIDIA detection
        if (description.Contains("nvidia") || 
            description.Contains("geforce") || 
            description.Contains("rtx") || 
            description.Contains("gtx") || 
            description.Contains("quadro") ||
            description.Contains("tesla"))
        {
            return GpuManufacturer.Nvidia;
        }

        // AMD detection
        if (description.Contains("amd") || 
            description.Contains("radeon") || 
            description.Contains("rx ") || 
            description.Contains("vega") ||
            description.Contains("navi") ||
            description.Contains("rdna"))
        {
            return GpuManufacturer.Amd;
        }

        // Intel detection
        if (description.Contains("intel") || 
            description.Contains("iris") || 
            description.Contains("uhd") ||
            description.Contains("hd graphics"))
        {
            return GpuManufacturer.Intel;
        }

        return GpuManufacturer.Unknown;
    }

    /// <summary>
    /// Checks if a mod is specific to the detected GPU manufacturer
    /// </summary>
    /// <param name="modName">Name of the mod to check</param>
    /// <param name="gpuInfo">Detected GPU information</param>
    /// <returns>True if mod is GPU-specific for this manufacturer</returns>
    public bool IsModGpuSpecific(string modName, GpuInfo gpuInfo)
    {
        if (string.IsNullOrEmpty(modName) || gpuInfo.Manufacturer == GpuManufacturer.Unknown)
            return false;

        var modNameLower = modName.ToLowerInvariant();

        return gpuInfo.Manufacturer switch
        {
            GpuManufacturer.Nvidia => modNameLower.Contains("nvidia") || modNameLower.Contains("geforce"),
            GpuManufacturer.Amd => modNameLower.Contains("amd") || modNameLower.Contains("radeon"),
            _ => false
        };
    }

    /// <summary>
    /// Gets GPU compatibility warnings for a specific mod
    /// </summary>
    /// <param name="modName">Name of the mod</param>
    /// <param name="gpuInfo">Detected GPU information</param>
    /// <returns>GPU compatibility warning if applicable</returns>
    public string? GetGpuCompatibilityWarning(string modName, GpuInfo gpuInfo)
    {
        if (string.IsNullOrEmpty(modName) || gpuInfo.Manufacturer == GpuManufacturer.Unknown)
            return null;

        var modNameLower = modName.ToLowerInvariant();

        // Check for rival GPU specific mods
        if (gpuInfo.Manufacturer == GpuManufacturer.Nvidia && modNameLower.Contains("amd"))
        {
            return "⚠️ This AMD-specific mod may not work properly with your NVIDIA GPU";
        }

        if (gpuInfo.Manufacturer == GpuManufacturer.Amd && modNameLower.Contains("nvidia"))
        {
            return "⚠️ This NVIDIA-specific mod may not work properly with your AMD GPU";
        }

        return null;
    }
}
