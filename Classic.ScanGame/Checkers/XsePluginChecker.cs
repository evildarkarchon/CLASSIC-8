using System.IO.Abstractions;
using Classic.Core.Interfaces;
using Classic.Infrastructure;
using Serilog;

namespace Classic.ScanGame.Checkers;

/// <summary>
/// Checks XSE plugins for compatibility and addresses potential issues.
/// </summary>
public class XsePluginChecker : IXsePluginChecker
{
    private readonly IFileSystem _fileSystem;
    private readonly IYamlSettingsCache _yamlSettings;
    private readonly IGlobalRegistry _globalRegistry;
    private readonly ILogger _logger;

    public XsePluginChecker(
        IFileSystem fileSystem,
        IYamlSettingsCache yamlSettings,
        IGlobalRegistry globalRegistry,
        ILogger logger)
    {
        _fileSystem = fileSystem;
        _yamlSettings = yamlSettings;
        _globalRegistry = globalRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Address Library version information.
    /// </summary>
    public record AddressLibVersionInfo(
        string VersionConst,
        string Filename,
        string Description,
        string Url);

    private static readonly Dictionary<string, AddressLibVersionInfo> AllAddressLibInfo = new()
    {
        ["VR"] = new AddressLibVersionInfo(
            "VR_VERSION",
            "version-1-2-72-0.csv",
            "Virtual Reality (VR) version",
            "https://www.nexusmods.com/fallout4/mods/64879?tab=files"),
        ["OG"] = new AddressLibVersionInfo(
            "OG_VERSION",
            "version-1-10-163-0.bin",
            "Non-VR (Regular) version",
            "https://www.nexusmods.com/fallout4/mods/47327?tab=files"),
        ["NG"] = new AddressLibVersionInfo(
            "NG_VERSION",
            "version-1-10-984-0.bin",
            "Non-VR (New Game) version",
            "https://www.nexusmods.com/fallout4/mods/47327?tab=files")
    };

    /// <summary>
    /// Scans game files and configurations for issues.
    /// </summary>
    /// <returns>A task that represents the asynchronous scan operation. The task result contains a formatted message with scan results.</returns>
    public async Task<string> ScanAsync()
    {
        return await CheckXsePluginsAsync();
    }

    /// <summary>
    /// Checks the XSE plugins for compatibility and addresses potential issues.
    /// </summary>
    /// <returns>A message detailing the result of the compatibility check.</returns>
    public async Task<string> CheckXsePluginsAsync()
    {
        try
        {
            var pluginsPath = await GetPluginsPathAsync();
            if (pluginsPath == null)
            {
                return FormatPluginsPathNotFoundMessage();
            }

            var gameExePath = await GetGameExePathAsync();
            if (gameExePath == null)
            {
                return FormatGameVersionNotDetectedMessage();
            }

            var gameVersion = await GetGameVersionAsync(gameExePath);
            if (string.IsNullOrEmpty(gameVersion) || gameVersion == "NULL_VERSION")
            {
                return FormatGameVersionNotDetectedMessage();
            }

            var isVrMode = await GetVrModeAsync();
            var (correctVersions, wrongVersions) = DetermineRelevantVersions(isVrMode);

            var correctVersionExists = correctVersions.Any(version => 
                _fileSystem.File.Exists(_fileSystem.Path.Combine(pluginsPath, version.Filename)));
            var wrongVersionExists = wrongVersions.Any(version => 
                _fileSystem.File.Exists(_fileSystem.Path.Combine(pluginsPath, version.Filename)));

            if (correctVersionExists)
            {
                return FormatCorrectAddressLibMessage();
            }

            if (wrongVersionExists)
            {
                return FormatWrongAddressLibMessage(correctVersions[0]);
            }

            return FormatAddressLibNotFoundMessage(correctVersions[0]);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking XSE plugins");
            return "❌ ERROR: Failed to check XSE plugins compatibility\n-----\n";
        }
    }

    private async Task<string?> GetPluginsPathAsync()
    {
        var isVrMode = await GetVrModeAsync();
        var vrSuffix = isVrMode ? "VR" : "";
        var key = $"Game{vrSuffix}_Info.Game_Folder_Plugins";
        return await _yamlSettings.GetSettingAsync<string>("Game_Local", key);
    }

    private async Task<string?> GetGameExePathAsync()
    {
        var isVrMode = await GetVrModeAsync();
        var vrSuffix = isVrMode ? "VR" : "";
        var key = $"Game{vrSuffix}_Info.Game_File_EXE";
        return await _yamlSettings.GetSettingAsync<string>("Game_Local", key);
    }

    private async Task<string> GetGameVersionAsync(string gameExePath)
    {
        // This would normally call a utility to get the game version from the EXE
        // For now, return a placeholder
        await Task.CompletedTask;
        return "1.10.163.0"; // Placeholder
    }

    private async Task<bool> GetVrModeAsync()
    {
        var vrMode = await _yamlSettings.GetSettingAsync<bool?>("Classic", "VR Mode");
        return vrMode ?? false;
    }

    private static (List<AddressLibVersionInfo> correct, List<AddressLibVersionInfo> wrong) 
        DetermineRelevantVersions(bool isVrMode)
    {
        if (isVrMode)
        {
            return (
                [AllAddressLibInfo["VR"]], 
                [AllAddressLibInfo["OG"], AllAddressLibInfo["NG"]]);
        }

        return (
            [AllAddressLibInfo["OG"], AllAddressLibInfo["NG"]], 
            [AllAddressLibInfo["VR"]]);
    }

    private static string FormatGameVersionNotDetectedMessage()
    {
        return "❓ NOTICE : Unable to locate Address Library\n" +
               "  If you have Address Library installed, please check the path in your settings.\n" +
               "  If you don't have it installed, you can find it on the Nexus.\n" +
               $"  Link: Regular: {AllAddressLibInfo["OG"].Url} or VR: {AllAddressLibInfo["VR"].Url}\n-----\n";
    }

    private static string FormatPluginsPathNotFoundMessage()
    {
        return "❌ ERROR: Could not locate plugins folder path in settings\n-----\n";
    }

    private static string FormatCorrectAddressLibMessage()
    {
        return "✔️ You have the correct version of the Address Library file!\n-----\n";
    }

    private static string FormatWrongAddressLibMessage(AddressLibVersionInfo correctVersionInfo)
    {
        return "❌ CAUTION: You have installed the wrong version of the Address Library file!\n" +
               $"  Remove the current Address Library file and install the {correctVersionInfo.Description}.\n" +
               $"  Link: {correctVersionInfo.Url}\n-----\n";
    }

    private static string FormatAddressLibNotFoundMessage(AddressLibVersionInfo correctVersionInfo)
    {
        return "❓ NOTICE: Address Library file not found\n" +
               $"  Please install the {correctVersionInfo.Description} for proper functionality.\n" +
               $"  Link: {correctVersionInfo.Url}\n-----\n";
    }
}