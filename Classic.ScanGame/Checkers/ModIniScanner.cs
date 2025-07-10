using System.IO.Abstractions;
using Classic.Core.Interfaces;
using Classic.Infrastructure;
using Classic.ScanGame.Configuration;
using Serilog;

namespace Classic.ScanGame.Checkers;

/// <summary>
/// Scans and processes INI files for mods, performing necessary fixes or notifications about potential issues.
/// </summary>
public class ModIniScanner : IModIniScanner
{
    private readonly IFileSystem _fileSystem;
    private readonly IYamlSettingsCache _yamlSettings;
    private readonly IGameConfiguration _gameConfiguration;
    private readonly ILogger _logger;

    // Constants for config settings
    private const string ConsoleCommandSetting = "sStartingConsoleCommand";
    private const string ConsoleCommandSection = "General";
    private const string ConsoleCommandNotice = 
        "In rare cases, this setting can slow down the initial game startup time for some players.\n" +
        "You can test your initial startup time difference by removing this setting from the INI file.\n-----\n";

    public ModIniScanner(
        IFileSystem fileSystem,
        IYamlSettingsCache yamlSettings,
        IGameConfiguration gameConfiguration,
        ILogger logger)
    {
        _fileSystem = fileSystem;
        _yamlSettings = yamlSettings;
        _gameConfiguration = gameConfiguration;
        _logger = logger;
    }

    /// <summary>
    /// Scans game files and configurations for issues.
    /// </summary>
    /// <returns>A task that represents the asynchronous scan operation. The task result contains a formatted message with scan results.</returns>
    public async Task<string> ScanAsync()
    {
        return await ScanModInisAsync();
    }

    /// <summary>
    /// Scans INI files for mods and performs necessary fixes or notifications about potential issues.
    /// </summary>
    /// <returns>A concatenated string of messages highlighting changes, issues, or notices.</returns>
    public async Task<string> ScanModInisAsync()
    {
        try
        {
            var messageList = new List<string>();
            var configFiles = await ConfigFileCache.CreateAsync(_fileSystem, _yamlSettings, _gameConfiguration, _logger);

            // Check for console command settings that might slow down startup
            await CheckStartingConsoleCommandAsync(configFiles, messageList);

            // Check for VSync settings in various files
            var vsyncList = await CheckVsyncSettingsAsync(configFiles);

            // Apply fixes to various INI files
            await ApplyAllIniFixesAsync(configFiles, messageList);

            // Report VSync settings if found
            if (vsyncList.Count > 0)
            {
                messageList.Add("* NOTICE : VSYNC IS CURRENTLY ENABLED IN THE FOLLOWING FILES *\n");
                messageList.AddRange(vsyncList);
            }

            // Report duplicate files if found
            await CheckDuplicateFilesAsync(configFiles, messageList);

            return string.Join("", messageList);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error scanning mod INI files");
            return "❌ ERROR: Failed to scan mod INI files\n-----\n";
        }
    }

    /// <summary>
    /// Checks for console command settings that might slow down game startup.
    /// </summary>
    private async Task CheckStartingConsoleCommandAsync(ConfigFileCache configFiles, List<string> messageList)
    {
        var gameLower = _gameConfiguration.CurrentGame.ToLowerInvariant();

        foreach (var (fileName, filePath) in configFiles.GetFiles())
        {
            if (fileName.StartsWith(gameLower, StringComparison.OrdinalIgnoreCase) && 
                await configFiles.HasSettingAsync(fileName, ConsoleCommandSection, ConsoleCommandSetting))
            {
                messageList.AddRange(new[]
                {
                    $"[!] NOTICE: {filePath} contains the *{ConsoleCommandSetting}* setting.\n",
                    ConsoleCommandNotice
                });
            }
        }
    }

    /// <summary>
    /// Checks for VSync settings in various configuration files.
    /// </summary>
    private async Task<List<string>> CheckVsyncSettingsAsync(ConfigFileCache configFiles)
    {
        var vsyncList = new List<string>();

        // List of files and their VSync settings to check
        var vsyncSettings = new List<VsyncSetting>
        {
            new("dxvk.conf", $"{_gameConfiguration.CurrentGame}.exe", "dxgi.syncInterval"),
            new("enblocal.ini", "ENGINE", "ForceVSync"),
            new("longloadingtimesfix.ini", "Limiter", "EnableVSync"),
            new("reshade.ini", "APP", "ForceVsync"),
            new("fallout4_test.ini", "CreationKit", "VSyncRender")
        };

        // Check standard VSync settings
        foreach (var setting in vsyncSettings)
        {
            var value = await configFiles.GetSettingAsync<bool?>(setting.FileName, setting.Section, setting.Setting);
            if (value == true)
            {
                var filePath = configFiles.GetFilePath(setting.FileName);
                vsyncList.Add($"{filePath} | SETTING: {setting.Setting}\n");
            }
        }

        // Check highfpsphysicsfix.ini separately
        var highFpsValue = await configFiles.GetSettingAsync<bool?>("highfpsphysicsfix.ini", "Main", "EnableVSync");
        if (highFpsValue == true)
        {
            var filePath = configFiles.GetFilePath("highfpsphysicsfix.ini");
            vsyncList.Add($"{filePath} | SETTING: EnableVSync\n");
        }

        return vsyncList;
    }

    /// <summary>
    /// Applies a fix to a configuration file by updating its settings and logs the operation.
    /// </summary>
    private async Task ApplyIniFixAsync(
        ConfigFileCache configFiles,
        string fileName,
        string section,
        string setting,
        object value,
        string fixDescription,
        List<string> messageList)
    {
        await configFiles.SetSettingAsync(fileName, section, setting, value);
        var filePath = configFiles.GetFilePath(fileName);
        _logger.Information("> > > PERFORMED {FixDescription} FIX FOR {FilePath}", fixDescription, filePath);
        messageList.Add($"> Performed {fixDescription.ToTitleCase()} Fix For : {filePath}\n");
    }

    /// <summary>
    /// Applies all necessary fixes to the specified configuration files.
    /// </summary>
    private async Task ApplyAllIniFixesAsync(ConfigFileCache configFiles, List<string> messageList)
    {
        // Fix ESPExplorer hotkey
        var hotkey = await configFiles.GetSettingAsync<string>("espexplorer.ini", "General", "HotKey");
        if (hotkey?.Contains("; F10") == true)
        {
            await ApplyIniFixAsync(configFiles, "espexplorer.ini", "General", "HotKey", "0x79", "INI HOTKEY", messageList);
        }

        // Fix EPO particle count
        var particleCount = await configFiles.GetSettingAsync<int?>("epo.ini", "Particles", "iMaxDesired");
        if (particleCount > 5000)
        {
            await ApplyIniFixAsync(configFiles, "epo.ini", "Particles", "iMaxDesired", 5000, "INI PARTICLE COUNT", messageList);
        }

        // Fix F4EE settings if present
        if (configFiles.HasFile("f4ee.ini"))
        {
            // Fix head parts unlock setting
            var headParts = await configFiles.GetSettingAsync<int?>("f4ee.ini", "CharGen", "bUnlockHeadParts");
            if (headParts == 0)
            {
                await ApplyIniFixAsync(configFiles, "f4ee.ini", "CharGen", "bUnlockHeadParts", 1, "INI HEAD PARTS UNLOCK", messageList);
            }

            // Fix face tints unlock setting
            var faceTints = await configFiles.GetSettingAsync<int?>("f4ee.ini", "CharGen", "bUnlockTints");
            if (faceTints == 0)
            {
                await ApplyIniFixAsync(configFiles, "f4ee.ini", "CharGen", "bUnlockTints", 1, "INI FACE TINTS UNLOCK", messageList);
            }
        }

        // Fix highfpsphysicsfix.ini loading screen FPS if present
        if (configFiles.HasFile("highfpsphysicsfix.ini"))
        {
            var loadingScreenFps = await configFiles.GetSettingAsync<double?>("highfpsphysicsfix.ini", "Limiter", "LoadingScreenFPS");
            if (loadingScreenFps < 600.0)
            {
                await ApplyIniFixAsync(configFiles, "highfpsphysicsfix.ini", "Limiter", "LoadingScreenFPS", 600.0, "INI LOADING SCREEN FPS", messageList);
            }
        }
    }

    /// <summary>
    /// Checks for duplicate files in the configuration files and updates the message list.
    /// </summary>
    private async Task CheckDuplicateFilesAsync(ConfigFileCache configFiles, List<string> messageList)
    {
        var duplicateFiles = await configFiles.GetDuplicateFilesAsync();
        if (duplicateFiles.Count > 0)
        {
            var allDuplicates = new List<string>();

            // Collect paths from duplicate files
            foreach (var paths in duplicateFiles.Values)
            {
                allDuplicates.AddRange(paths);
            }

            // Also add original files that have duplicates
            foreach (var fileName in duplicateFiles.Keys)
            {
                var originalPath = configFiles.GetFilePath(fileName);
                if (!string.IsNullOrEmpty(originalPath))
                {
                    allDuplicates.Add(originalPath);
                }
            }

            // Sort by filename for consistent output
            var sortedDuplicates = allDuplicates.OrderBy(p => _fileSystem.Path.GetFileName(p));

            messageList.Add("* NOTICE : DUPLICATES FOUND OF THE FOLLOWING FILES *\n");
            messageList.AddRange(sortedDuplicates.Select(p => $"{p}\n"));
        }
    }

    /// <summary>
    /// Represents a VSync setting configuration.
    /// </summary>
    private record VsyncSetting(string FileName, string Section, string Setting);
}

/// <summary>
/// Extension methods for string operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to title case.
    /// </summary>
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
            }
        }

        return string.Join(" ", words);
    }
}